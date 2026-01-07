using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.ArrStopMonitoring.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ArrStopMonitoring.Services;

/// <summary>
/// Service for interacting with the Radarr API.
/// </summary>
public class RadarrService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RadarrService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadarrService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public RadarrService(HttpClient httpClient, ILogger<RadarrService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Unmonitors a movie in Radarr by its TMDB ID.
    /// </summary>
    /// <param name="tmdbId">The TMDB ID of the movie.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> UnmonitorMovieAsync(int tmdbId)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration is null");
            return false;
        }

        try
        {
            // Step 1: Find the movie by TMDB ID
            var movie = await GetMovieByTmdbIdAsync(tmdbId, config.RadarrUrl, config.RadarrApiKey);
            if (movie == null)
            {
                _logger.LogWarning("Movie with TMDB ID {TmdbId} not found in Radarr", tmdbId);
                return false;
            }

            if (!movie.Monitored)
            {
                _logger.LogDebug("Movie '{Title}' (TMDB: {TmdbId}) is already unmonitored in Radarr",
                    movie.Title, tmdbId);
                return true;
            }

            // Step 2: Update the movie to unmonitored
            movie.Monitored = false;

            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{config.RadarrUrl}/api/v3/movie/{movie.Id}");
            request.Headers.Add("X-Api-Key", config.RadarrApiKey);
            request.Content = JsonContent.Create(movie);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully unmonitored movie '{Title}' (TMDB: {TmdbId}) in Radarr",
                movie.Title, tmdbId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while unmonitoring movie with TMDB ID {TmdbId} in Radarr", tmdbId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unmonitor movie with TMDB ID {TmdbId} in Radarr", tmdbId);
            return false;
        }
    }

    /// <summary>
    /// Tests the connection to Radarr.
    /// </summary>
    /// <param name="url">The Radarr URL.</param>
    /// <param name="apiKey">The API key.</param>
    /// <returns>The system status if successful, null otherwise.</returns>
    public async Task<RadarrSystemStatus?> TestConnectionAsync(string url, string apiKey)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{url}/api/v3/system/status");
            request.Headers.Add("X-Api-Key", apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<RadarrSystemStatus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Radarr connection");
            return null;
        }
    }

    private async Task<RadarrMovie?> GetMovieByTmdbIdAsync(int tmdbId, string radarrUrl, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{radarrUrl}/api/v3/movie?tmdbId={tmdbId}");
        request.Headers.Add("X-Api-Key", apiKey);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var movies = await response.Content.ReadFromJsonAsync<List<RadarrMovie>>();
        return movies?.Count > 0 ? movies[0] : null;
    }
}


