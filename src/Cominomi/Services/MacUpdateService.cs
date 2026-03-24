using System.Text.Json;
using Cominomi.Shared.Services;
using Microsoft.Extensions.Logging;

using AppUpdateInfo = Cominomi.Shared.Services.UpdateInfo;

namespace Cominomi.Services;

public class MacUpdateService : IUpdateService
{
    private readonly ILogger<MacUpdateService> _logger;
    private readonly HttpClient _httpClient;
    private string? _downloadUrl;

    public MacUpdateService(ILogger<MacUpdateService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Cominomi");

        var token = Environment.GetEnvironmentVariable("COMINOMI_GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public bool IsInstalled =>
        AppContext.BaseDirectory.Contains("/Applications/", StringComparison.Ordinal);

    public bool CanApplyUpdate => false;

    public async Task<AppUpdateInfo?> CheckForUpdateAsync()
    {
        if (!IsInstalled)
        {
            _logger.LogDebug("Update check skipped: app not running from Applications folder");
            return null;
        }

        try
        {
            var response = await _httpClient.GetAsync(
                "https://api.github.com/repos/JinoPay/Cominomi/releases/latest");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("GitHub API returned {Status}", response.StatusCode);
                return null;
            }

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());
            var root = json.RootElement;

            var tagName = root.GetProperty("tag_name").GetString();
            if (string.IsNullOrEmpty(tagName)) return null;

            var latestVersion = tagName.TrimStart('v');
            var currentVersion = AppInfo.Current.VersionString;

            if (!IsNewerVersion(latestVersion, currentVersion))
            {
                _logger.LogDebug("No update (current: {Current}, latest: {Latest})",
                    currentVersion, latestVersion);
                return null;
            }

            // Default to the release page
            _downloadUrl = root.GetProperty("html_url").GetString();
            long? downloadSize = null;

            // Try to find a macOS installer asset (.pkg preferred, then .zip)
            if (root.TryGetProperty("assets", out var assets))
            {
                string? zipUrl = null;
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (!name.Contains("osx", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var url = asset.GetProperty("browser_download_url").GetString();
                    var size = asset.GetProperty("size").GetInt64();

                    if (name.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase))
                    {
                        _downloadUrl = url;
                        downloadSize = size;
                        break;
                    }

                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        zipUrl = url;
                        downloadSize = size;
                    }
                }

                if (zipUrl != null && _downloadUrl == root.GetProperty("html_url").GetString())
                {
                    _downloadUrl = zipUrl;
                }
            }

            _logger.LogInformation("Update available: {Version}", latestVersion);
            return new AppUpdateInfo(latestVersion, downloadSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
            return null;
        }
    }

    public Task DownloadUpdateAsync()
    {
        if (!string.IsNullOrEmpty(_downloadUrl))
        {
            _logger.LogInformation("Opening download: {Url}", _downloadUrl);
            _ = Launcher.Default.OpenAsync(new Uri(_downloadUrl));
        }

        return Task.CompletedTask;
    }

    public void ApplyUpdateAndRestart()
    {
        // Not supported on Mac Catalyst — user installs manually after download
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        return Version.TryParse(latest, out var latestVer) &&
               Version.TryParse(current, out var currentVer) &&
               latestVer > currentVer;
    }
}
