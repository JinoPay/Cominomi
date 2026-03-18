using System.Text.Json;
using Cominomi.Shared.Models;
using Cominomi.Shared.Services.Migration;
using Microsoft.Extensions.Options;

namespace Cominomi.Shared.Services;

/// <summary>
/// Loads AppSettings from the JSON settings file each time IOptionsMonitor needs a fresh instance.
/// </summary>
public class AppSettingsFactory : IOptionsFactory<AppSettings>
{
    public AppSettings Create(string name)
    {
        var path = AppPaths.SettingsFile;
        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(path);
            var (migrated, _) = JsonMigrator.MigrateJson("AppSettings", json);
            var settings = JsonSerializer.Deserialize<AppSettings>(migrated, JsonDefaults.Options) ?? new AppSettings();
            settings.DefaultModel = ModelDefinitions.NormalizeModelId(settings.DefaultModel);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }
}
