using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ArrStopMonitoring.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        RadarrUrl = "http://localhost:7878";
        RadarrApiKey = string.Empty;
        RadarrEnabled = true;

        SonarrUrl = "http://localhost:8989";
        SonarrApiKey = string.Empty;
        SonarrEnabled = true;
        AutoUnmonitorCompletedSeasons = true;

        TrackedUsernames = Array.Empty<string>();
        WatchThreshold = 0.90;
        DryRun = false;

        TriggerOnPlayback = true;
        TriggerOnManuallyMarkedWatched = false;
    }

    // ==================== Radarr Settings ====================

    /// <summary>
    /// Gets or sets the Radarr server URL.
    /// </summary>
    public string RadarrUrl { get; set; }

    /// <summary>
    /// Gets or sets the Radarr API key.
    /// </summary>
    public string RadarrApiKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Radarr integration is enabled.
    /// </summary>
    public bool RadarrEnabled { get; set; }

    // ==================== Sonarr Settings ====================

    /// <summary>
    /// Gets or sets the Sonarr server URL.
    /// </summary>
    public string SonarrUrl { get; set; }

    /// <summary>
    /// Gets or sets the Sonarr API key.
    /// </summary>
    public string SonarrApiKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Sonarr integration is enabled.
    /// </summary>
    public bool SonarrEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically unmonitor a season
    /// when all episodes in it have been watched.
    /// </summary>
    public bool AutoUnmonitorCompletedSeasons { get; set; }

    // ==================== General Settings ====================

    /// <summary>
    /// Gets or sets the usernames to track (case-insensitive).
    /// Empty array means track all users.
    /// </summary>
    public string[] TrackedUsernames { get; set; }

    /// <summary>
    /// Gets or sets the watch threshold (0.0 to 1.0) to consider media as "watched".
    /// Default is 0.90 (90%).
    /// </summary>
    public double WatchThreshold { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether dry run mode is enabled.
    /// When enabled, actions are logged but not executed.
    /// </summary>
    public bool DryRun { get; set; }

    // ==================== Trigger Settings ====================

    /// <summary>
    /// Gets or sets a value indicating whether to trigger unmonitoring when media is watched via playback.
    /// </summary>
    public bool TriggerOnPlayback { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to trigger unmonitoring when media is manually marked as watched.
    /// </summary>
    public bool TriggerOnManuallyMarkedWatched { get; set; }
}


