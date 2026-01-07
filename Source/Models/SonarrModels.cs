using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ArrStopMonitoring.Models;

/// <summary>
/// Represents a TV series in Sonarr.
/// </summary>
public class SonarrSeries
{
    /// <summary>
    /// Gets or sets the Sonarr internal ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TVDB ID.
    /// </summary>
    [JsonPropertyName("tvdbId")]
    public int TvdbId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the series is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets the list of seasons.
    /// </summary>
    [JsonPropertyName("seasons")]
    public List<SonarrSeason>? Seasons { get; set; }

    /// <summary>
    /// Gets or sets additional data to preserve during updates.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>
/// Represents a season within a Sonarr series.
/// </summary>
public class SonarrSeason
{
    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the season is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets additional data to preserve during updates.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>
/// Represents an episode in Sonarr.
/// </summary>
public class SonarrEpisode
{
    /// <summary>
    /// Gets or sets the Sonarr internal episode ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the series ID this episode belongs to.
    /// </summary>
    [JsonPropertyName("seriesId")]
    public int SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the TVDB episode ID.
    /// </summary>
    [JsonPropertyName("tvdbId")]
    public int TvdbId { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number within the season.
    /// </summary>
    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the episode is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the episode has a file.
    /// </summary>
    [JsonPropertyName("hasFile")]
    public bool HasFile { get; set; }
}

/// <summary>
/// Request body for the Sonarr episode monitor endpoint.
/// </summary>
public class SonarrEpisodeMonitorRequest
{
    /// <summary>
    /// Gets or sets the episode IDs to update.
    /// </summary>
    [JsonPropertyName("episodeIds")]
    public int[] EpisodeIds { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Gets or sets a value indicating whether to monitor or unmonitor.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }
}

/// <summary>
/// Represents the Sonarr system status response.
/// </summary>
public class SonarrSystemStatus
{
    /// <summary>
    /// Gets or sets the Sonarr version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}


