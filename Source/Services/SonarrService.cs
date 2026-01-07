using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.ArrStopMonitoring.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ArrStopMonitoring.Services;

/// <summary>
/// Service for interacting with the Sonarr API.
/// </summary>
public class SonarrService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SonarrService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SonarrService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public SonarrService(HttpClient httpClient, ILogger<SonarrService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Unmonitors an episode in Sonarr and optionally the season if all episodes are watched.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID of the series.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="autoUnmonitorSeason">Whether to auto-unmonitor the season when complete.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> UnmonitorEpisodeAsync(int tvdbId, int seasonNumber, int episodeNumber, bool autoUnmonitorSeason)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration is null");
            return false;
        }

        try
        {
            // Step 1: Find the series by TVDB ID
            var series = await GetSeriesByTvdbIdAsync(tvdbId, config.SonarrUrl, config.SonarrApiKey);
            if (series == null)
            {
                _logger.LogWarning("Series with TVDB ID {TvdbId} not found in Sonarr", tvdbId);
                return false;
            }

            // Step 2: Unmonitor the specific episode
            var episodeUnmonitored = await UnmonitorSpecificEpisodeAsync(
                series.Id, seasonNumber, episodeNumber, config.SonarrUrl, config.SonarrApiKey);

            if (!episodeUnmonitored)
            {
                return false;
            }

            // Step 3: Check if we should unmonitor the entire season
            if (autoUnmonitorSeason)
            {
                await TryUnmonitorSeasonIfCompleteAsync(series, seasonNumber, config.SonarrUrl, config.SonarrApiKey);
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while unmonitoring episode TVDB:{TvdbId} S{Season}E{Episode} in Sonarr",
                tvdbId, seasonNumber, episodeNumber);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unmonitor episode TVDB:{TvdbId} S{Season}E{Episode} in Sonarr",
                tvdbId, seasonNumber, episodeNumber);
            return false;
        }
    }

    /// <summary>
    /// Tests the connection to Sonarr.
    /// </summary>
    /// <param name="url">The Sonarr URL.</param>
    /// <param name="apiKey">The API key.</param>
    /// <returns>The system status if successful, null otherwise.</returns>
    public async Task<SonarrSystemStatus?> TestConnectionAsync(string url, string apiKey)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{url}/api/v3/system/status");
            request.Headers.Add("X-Api-Key", apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<SonarrSystemStatus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Sonarr connection");
            return null;
        }
    }

    private async Task<SonarrSeries?> GetSeriesByTvdbIdAsync(int tvdbId, string sonarrUrl, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{sonarrUrl}/api/v3/series?tvdbId={tvdbId}");
        request.Headers.Add("X-Api-Key", apiKey);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var seriesList = await response.Content.ReadFromJsonAsync<List<SonarrSeries>>();
        return seriesList?.Count > 0 ? seriesList[0] : null;
    }

    private async Task<List<SonarrEpisode>> GetEpisodesForSeriesAsync(int seriesId, string sonarrUrl, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{sonarrUrl}/api/v3/episode?seriesId={seriesId}");
        request.Headers.Add("X-Api-Key", apiKey);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<SonarrEpisode>>() ?? new List<SonarrEpisode>();
    }

    private async Task<bool> UnmonitorSpecificEpisodeAsync(
        int seriesId,
        int seasonNumber,
        int episodeNumber,
        string sonarrUrl,
        string apiKey)
    {
        // Get all episodes for the series
        var episodes = await GetEpisodesForSeriesAsync(seriesId, sonarrUrl, apiKey);

        var episode = episodes.FirstOrDefault(e =>
            e.SeasonNumber == seasonNumber && e.EpisodeNumber == episodeNumber);

        if (episode == null)
        {
            _logger.LogWarning("Episode S{Season:D2}E{Episode:D2} not found in Sonarr for series ID {SeriesId}",
                seasonNumber, episodeNumber, seriesId);
            return false;
        }

        if (!episode.Monitored)
        {
            _logger.LogDebug("Episode S{Season:D2}E{Episode:D2} is already unmonitored in Sonarr",
                seasonNumber, episodeNumber);
            return true;
        }

        // Use the bulk monitor endpoint to unmonitor the episode
        var monitorRequest = new HttpRequestMessage(HttpMethod.Put,
            $"{sonarrUrl}/api/v3/episode/monitor");
        monitorRequest.Headers.Add("X-Api-Key", apiKey);
        monitorRequest.Content = JsonContent.Create(new SonarrEpisodeMonitorRequest
        {
            EpisodeIds = new[] { episode.Id },
            Monitored = false
        });

        var response = await _httpClient.SendAsync(monitorRequest);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Successfully unmonitored episode S{Season:D2}E{Episode:D2} in Sonarr",
            seasonNumber, episodeNumber);
        return true;
    }

    private async Task TryUnmonitorSeasonIfCompleteAsync(
        SonarrSeries series,
        int seasonNumber,
        string sonarrUrl,
        string apiKey)
    {
        // Get fresh episode data
        var episodes = await GetEpisodesForSeriesAsync(series.Id, sonarrUrl, apiKey);

        // Filter to episodes in this season that have files (aired and downloaded)
        var seasonEpisodes = episodes
            .Where(e => e.SeasonNumber == seasonNumber && e.HasFile)
            .ToList();

        if (seasonEpisodes.Count == 0)
        {
            _logger.LogDebug("No downloaded episodes found for season {Season}", seasonNumber);
            return;
        }

        // Check if all downloaded episodes in the season are unmonitored
        var allUnmonitored = seasonEpisodes.All(e => !e.Monitored);

        if (!allUnmonitored)
        {
            _logger.LogDebug("Not all episodes in season {Season} are unmonitored yet ({Unmonitored}/{Total})",
                seasonNumber,
                seasonEpisodes.Count(e => !e.Monitored),
                seasonEpisodes.Count);
            return;
        }

        // All episodes are unmonitored - unmonitor the season
        _logger.LogInformation(
            "All {Count} downloaded episodes in '{Series}' Season {Season} are unmonitored - unmonitoring season",
            seasonEpisodes.Count, series.Title, seasonNumber);

        await UnmonitorSeasonAsync(series, seasonNumber, sonarrUrl, apiKey);
    }

    private async Task UnmonitorSeasonAsync(SonarrSeries series, int seasonNumber, string sonarrUrl, string apiKey)
    {
        // Find and update the season's monitored status
        var season = series.Seasons?.FirstOrDefault(s => s.SeasonNumber == seasonNumber);
        if (season == null)
        {
            _logger.LogWarning("Season {Season} not found in series '{Series}'", seasonNumber, series.Title);
            return;
        }

        if (!season.Monitored)
        {
            _logger.LogDebug("Season {Season} of '{Series}' is already unmonitored", seasonNumber, series.Title);
            return;
        }

        season.Monitored = false;

        var request = new HttpRequestMessage(HttpMethod.Put,
            $"{sonarrUrl}/api/v3/series/{series.Id}");
        request.Headers.Add("X-Api-Key", apiKey);
        request.Content = JsonContent.Create(series);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Successfully unmonitored Season {Season} of '{Series}' in Sonarr",
            seasonNumber, series.Title);
    }
}


