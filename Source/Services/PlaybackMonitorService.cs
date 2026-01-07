using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ArrStopMonitoring.Services;

/// <summary>
/// Hosted service that monitors playback events and triggers unmonitoring in Radarr/Sonarr.
/// </summary>
public class PlaybackMonitorService : IHostedService, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<PlaybackMonitorService> _logger;
    private readonly RadarrService _radarrService;
    private readonly SonarrService _sonarrService;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackMonitorService"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public PlaybackMonitorService(
        ISessionManager sessionManager,
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory)
    {
        _sessionManager = sessionManager;
        _logger = loggerFactory.CreateLogger<PlaybackMonitorService>();
        _httpClient = httpClientFactory.CreateClient();
        _radarrService = new RadarrService(_httpClient, loggerFactory.CreateLogger<RadarrService>());
        _sonarrService = new SonarrService(_httpClient, loggerFactory.CreateLogger<SonarrService>());
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _logger.LogInformation("Arr Stop Monitoring plugin started - listening for playback events");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _logger.LogInformation("Arr Stop Monitoring plugin stopped");
        return Task.CompletedTask;
    }

    private async void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        try
        {
            await HandlePlaybackStoppedAsync(e).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback stopped event");
        }
    }

    private async Task HandlePlaybackStoppedAsync(PlaybackStopEventArgs e)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogWarning("Plugin configuration is null, skipping playback event");
            return;
        }

        // Get the users from the event
        if (e.Users == null || e.Users.Count == 0)
        {
            _logger.LogDebug("No users in playback event, skipping");
            return;
        }

        // Check if any of the users should be tracked (by username, case-insensitive)
        var trackedUser = e.Users.FirstOrDefault(u => ShouldTrackUser(u.Username, config.TrackedUsernames));
        if (trackedUser == null)
        {
            _logger.LogDebug("No tracked users in playback event, skipping");
            return;
        }

        _logger.LogDebug("Processing playback event for user '{Username}'", trackedUser.Username);

        // Check if the item was watched enough
        if (!IsWatchedEnough(e, config.WatchThreshold))
        {
            var progress = e.Item?.RunTimeTicks > 0
                ? (double)(e.PlaybackPositionTicks ?? 0) / e.Item.RunTimeTicks.Value * 100
                : 0;
            _logger.LogDebug(
                "Item '{Name}' not watched enough ({Progress:F1}% < {Threshold}%), skipping",
                e.Item?.Name,
                progress,
                config.WatchThreshold * 100);
            return;
        }

        var item = e.Item;
        if (item == null)
        {
            _logger.LogDebug("No item in playback event, skipping");
            return;
        }

        // Handle Movies → Radarr
        if (item is Movie movie && config.RadarrEnabled)
        {
            await HandleMovieWatchedAsync(movie, config.DryRun).ConfigureAwait(false);
        }
        // Handle Episodes → Sonarr
        else if (item is Episode episode && config.SonarrEnabled)
        {
            await HandleEpisodeWatchedAsync(episode, config.DryRun, config.AutoUnmonitorCompletedSeasons)
                .ConfigureAwait(false);
        }
    }

    private static bool ShouldTrackUser(string? username, string[] trackedUsernames)
    {
        // If no usernames specified, track all users
        if (trackedUsernames == null || trackedUsernames.Length == 0)
        {
            return true;
        }

        if (string.IsNullOrEmpty(username))
        {
            return false;
        }

        // Case-insensitive comparison
        return trackedUsernames.Any(trackedName =>
            string.Equals(trackedName, username, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsWatchedEnough(PlaybackStopEventArgs e, double threshold)
    {
        // If marked as played to completion, it's watched
        if (e.PlayedToCompletion)
        {
            return true;
        }

        // Calculate progress based on position vs runtime
        if (e.Item?.RunTimeTicks == null || e.Item.RunTimeTicks == 0)
        {
            return false;
        }

        var progress = (double)(e.PlaybackPositionTicks ?? 0) / e.Item.RunTimeTicks.Value;
        return progress >= threshold;
    }

    private async Task HandleMovieWatchedAsync(Movie movie, bool dryRun)
    {
        // Get TMDB ID from Jellyfin's provider IDs
        if (!movie.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbIdString) ||
            !int.TryParse(tmdbIdString, out var tmdbId))
        {
            _logger.LogWarning(
                "Movie '{Name}' has no valid TMDB ID, cannot match to Radarr",
                movie.Name);
            return;
        }

        _logger.LogInformation(
            "Movie watched: '{Name}' (TMDB: {TmdbId})",
            movie.Name,
            tmdbId);

        if (dryRun)
        {
            _logger.LogInformation(
                "[DRY RUN] Would unmonitor movie '{Name}' (TMDB: {TmdbId}) in Radarr",
                movie.Name,
                tmdbId);
            return;
        }

        var success = await _radarrService.UnmonitorMovieAsync(tmdbId).ConfigureAwait(false);
        if (success)
        {
            _logger.LogInformation(
                "Successfully processed movie '{Name}' - unmonitored in Radarr",
                movie.Name);
        }
        else
        {
            _logger.LogWarning(
                "Failed to unmonitor movie '{Name}' in Radarr",
                movie.Name);
        }
    }

    private async Task HandleEpisodeWatchedAsync(Episode episode, bool dryRun, bool autoUnmonitorSeason)
    {
        // Get the series
        var series = episode.Series;
        if (series == null)
        {
            _logger.LogWarning(
                "Episode '{Name}' has no associated series, cannot process",
                episode.Name);
            return;
        }

        // Get TVDB ID from the series
        if (!series.TryGetProviderId(MetadataProvider.Tvdb, out var tvdbIdString) ||
            !int.TryParse(tvdbIdString, out var tvdbId))
        {
            _logger.LogWarning(
                "Series '{SeriesName}' has no valid TVDB ID, cannot match to Sonarr",
                series.Name);
            return;
        }

        var seasonNumber = episode.ParentIndexNumber ?? 0;
        var episodeNumber = episode.IndexNumber ?? 0;

        _logger.LogInformation(
            "Episode watched: '{Series}' S{Season:D2}E{Episode:D2} - '{EpisodeName}' (TVDB: {TvdbId})",
            series.Name,
            seasonNumber,
            episodeNumber,
            episode.Name,
            tvdbId);

        if (dryRun)
        {
            _logger.LogInformation(
                "[DRY RUN] Would unmonitor '{Series}' S{Season:D2}E{Episode:D2} in Sonarr",
                series.Name,
                seasonNumber,
                episodeNumber);
            return;
        }

        var success = await _sonarrService
            .UnmonitorEpisodeAsync(tvdbId, seasonNumber, episodeNumber, autoUnmonitorSeason)
            .ConfigureAwait(false);

        if (success)
        {
            _logger.LogInformation(
                "Successfully processed episode '{Series}' S{Season:D2}E{Episode:D2}",
                series.Name,
                seasonNumber,
                episodeNumber);
        }
        else
        {
            _logger.LogWarning(
                "Failed to unmonitor episode '{Series}' S{Season:D2}E{Episode:D2} in Sonarr",
                series.Name,
                seasonNumber,
                episodeNumber);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        GC.SuppressFinalize(this);
    }
}


