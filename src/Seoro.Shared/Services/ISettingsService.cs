using Seoro.Shared.Models;

namespace Seoro.Shared.Services;

public interface ISettingsService
{
    event Action<AppSettings>? OnSettingsChanged;
    Task SaveAsync(AppSettings settings);
    Task<AppSettings> LoadAsync();
}