using Microsoft.Extensions.Logging;

namespace Seoro.Shared.Services.Cli;

/// <summary>
///     <see cref="ICliAvailabilityService" /> 구현체.
///     모든 프로바이더의 DetectCliAsync 결과를 캐싱하여 중복 감지를 방지한다.
/// </summary>
public class CliAvailabilityService(
    ICliProviderFactory factory,
    ILogger<CliAvailabilityService> logger)
    : ICliAvailabilityService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<string, bool>? _cache;

    public event Action? OnAvailabilityChanged;

    public async Task<bool> IsAvailableAsync(string providerId)
    {
        await EnsureDetectedAsync();
        return _cache!.GetValueOrDefault(providerId);
    }

    public async Task InvalidateAsync()
    {
        _cache = null;
        await EnsureDetectedAsync();
        OnAvailabilityChanged?.Invoke();
    }

    private async Task EnsureDetectedAsync()
    {
        if (_cache != null) return;

        await _lock.WaitAsync();
        try
        {
            if (_cache != null) return;

            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var provider in factory.GetAllProviders())
                try
                {
                    var (found, _) = await provider.DetectCliAsync();
                    result[provider.ProviderId] = found;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "{ProviderId} CLI 감지 실패", provider.ProviderId);
                    result[provider.ProviderId] = false;
                }

            _cache = result;
        }
        finally
        {
            _lock.Release();
        }
    }
}
