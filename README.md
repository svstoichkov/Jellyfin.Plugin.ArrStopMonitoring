# Jellyfin *Arr Stop Monitoring Plugin

Automatically unmonitor media in Radarr and Sonarr after you watch it in Jellyfin. This stops quality upgrades for content you've already watched.

## Features

- **Movies**: When you finish watching a movie, it's automatically unmonitored in Radarr
- **TV Episodes**: When you finish watching an episode, it's automatically unmonitored in Sonarr
- **Smart Season Completion**: Optionally unmonitor entire seasons when all episodes have been watched
- **User Filtering**: Only track specific users by username (case-insensitive), or track all users
- **Watch Threshold**: Configure what percentage counts as "watched" (default: 90%)
- **Dry Run Mode**: Test the plugin without making actual changes

## Installation

### Manual Installation

1. Download the latest release `.dll` file
2. Place it in your Jellyfin plugins directory:
   - Linux: `/var/lib/jellyfin/plugins/ArrStopMonitoring/`
   - Windows: `C:\ProgramData\Jellyfin\Server\plugins\ArrStopMonitoring\`
   - Docker: `/config/plugins/ArrStopMonitoring/`
3. Restart Jellyfin

### Building from Source

```bash
dotnet build -c Release
```

The compiled DLL will be in `bin/Release/net9.0/`

## Configuration

1. Go to Jellyfin Dashboard → Plugins → Arr Stop Monitoring
2. Configure your Radarr and Sonarr connections:
   - **URL**: The full URL including port (e.g., `http://localhost:7878`)
   - **API Key**: Found in Settings → General → Security in Radarr/Sonarr
3. Optionally configure:
   - **Watch Threshold**: Percentage of media that must be watched (default: 90%)
   - **Tracked Usernames**: Leave empty to track all users, or specify usernames (case-insensitive)
   - **Auto-unmonitor Seasons**: Unmonitor entire season when all episodes are watched
   - **Dry Run**: Enable to test without making changes

## How It Works

```
User finishes watching content
         ↓
Plugin checks:
  - Is this user being tracked? (by username, case-insensitive)
  - Was ≥90% of the content watched?
         ↓
For Movies:
  - Extract TMDB ID from Jellyfin metadata
  - Find movie in Radarr by TMDB ID
  - Set monitored=false
         ↓
For Episodes:
  - Extract TVDB ID from series metadata
  - Find episode in Sonarr
  - Set episode monitored=false
  - If all season episodes watched → unmonitor season
```

## Tracking Specific Users

To restrict the plugin to specific users, enter their Jellyfin usernames in the configuration:

```
john, Jane, admin
```

**Note**: Usernames are matched **case-insensitively**, so `john`, `John`, and `JOHN` all match the same user.

Leave the field empty to track all users.

## Logs

The plugin logs to Jellyfin's standard log. Look for entries containing `ArrStopMonitoring`.

Example log output:
```
[INF] Arr Stop Monitoring plugin started - listening for playback events
[INF] Processing playback event for user 'john'
[INF] Episode watched: 'Breaking Bad' S01E01 - 'Pilot' (TVDB: 81189)
[INF] Successfully unmonitored episode S01E01 in Sonarr
[INF] Movie watched: 'The Matrix' (TMDB: 603)
[INF] Successfully unmonitored movie 'The Matrix' (TMDB: 603) in Radarr
```

## Requirements

- Jellyfin 10.9.0 or later
- Radarr v3 API (Radarr 3.0+)
- Sonarr v3 API (Sonarr 3.0+)
- Media must have TMDB IDs (movies) or TVDB IDs (TV series) in Jellyfin metadata

## Troubleshooting

### Media not being unmonitored

1. Check that the media has the correct provider ID in Jellyfin (TMDB for movies, TVDB for series)
2. Verify the API connection using the "Test Connection" buttons in the config page
3. Enable Dry Run mode and check the logs to see what the plugin would do
4. Ensure the movie/series exists in Radarr/Sonarr
5. Verify your username is in the tracked list (or leave it empty to track everyone)

### Connection Test Fails

1. Verify the URL includes the protocol (`http://` or `https://`)
2. Ensure there's no trailing slash in the URL
3. Check that the API key is correct
4. If using Docker, ensure Jellyfin can reach Radarr/Sonarr (use container names or host IP)

## License

MIT License


