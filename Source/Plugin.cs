using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.ArrStopMonitoring.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.ArrStopMonitoring;

/// <summary>
/// Main plugin class for Arr Stop Monitoring.
/// Automatically unmonitors media in Radarr/Sonarr after watching.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Arr Stop Monitoring";

    /// <inheritdoc />
    public override string Description => "Automatically unmonitor media in Radarr/Sonarr after watching to stop quality upgrades";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("f8e3b2a1-5c4d-4e6f-9a8b-1c2d3e4f5a6b");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }
}


