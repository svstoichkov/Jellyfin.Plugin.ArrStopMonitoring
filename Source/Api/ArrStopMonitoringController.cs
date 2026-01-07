using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ArrStopMonitoring.Api;

/// <summary>
/// API controller for Arr Stop Monitoring plugin.
/// </summary>
[ApiController]
[Route("Plugins/ArrStopMonitoring")]
[Authorize]
public class ArrStopMonitoringController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrStopMonitoringController"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public ArrStopMonitoringController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Tests the connection to Radarr.
    /// </summary>
    /// <param name="url">The Radarr URL.</param>
    /// <param name="apiKey">The Radarr API key.</param>
    /// <returns>Connection test result.</returns>
    [HttpGet("TestRadarr")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestConnectionResult>> TestRadarr(
        [FromQuery][Required] string url,
        [FromQuery][Required] string apiKey)
    {
        return await TestConnectionAsync(url, apiKey, "Radarr");
    }

    /// <summary>
    /// Tests the connection to Sonarr.
    /// </summary>
    /// <param name="url">The Sonarr URL.</param>
    /// <param name="apiKey">The Sonarr API key.</param>
    /// <returns>Connection test result.</returns>
    [HttpGet("TestSonarr")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestConnectionResult>> TestSonarr(
        [FromQuery][Required] string url,
        [FromQuery][Required] string apiKey)
    {
        return await TestConnectionAsync(url, apiKey, "Sonarr");
    }

    private async Task<ActionResult<TestConnectionResult>> TestConnectionAsync(string url, string apiKey, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest(new TestConnectionResult
            {
                Success = false,
                Message = "URL and API key are required"
            });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = System.TimeSpan.FromSeconds(10);
            
            var requestUrl = $"{url.TrimEnd('/')}/api/v3/system/status";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("X-Api-Key", apiKey);

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<SystemStatusResponse>();
                return Ok(new TestConnectionResult
                {
                    Success = true,
                    Message = $"Connected to {serviceName} v{status?.Version ?? "unknown"}"
                });
            }

            return Ok(new TestConnectionResult
            {
                Success = false,
                Message = $"Error: {(int)response.StatusCode} {response.ReasonPhrase}"
            });
        }
        catch (HttpRequestException ex)
        {
            return Ok(new TestConnectionResult
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            });
        }
        catch (TaskCanceledException)
        {
            return Ok(new TestConnectionResult
            {
                Success = false,
                Message = "Connection timed out"
            });
        }
    }

    /// <summary>
    /// Response from system status endpoint.
    /// </summary>
    private class SystemStatusResponse
    {
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public string? Version { get; set; }
    }
}

/// <summary>
/// Result of a connection test.
/// </summary>
public class TestConnectionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the test was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}


