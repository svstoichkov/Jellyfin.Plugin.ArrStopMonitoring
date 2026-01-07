using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ArrStopMonitoring.Models;

/// <summary>
/// Represents a movie in Radarr.
/// </summary>
public class RadarrMovie
{
    /// <summary>
    /// Gets or sets the Radarr internal ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the movie is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets additional data to preserve during updates.
    /// This ensures we don't lose any fields when doing PUT requests.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>
/// Represents the Radarr system status response.
/// </summary>
public class RadarrSystemStatus
{
    /// <summary>
    /// Gets or sets the Radarr version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}


