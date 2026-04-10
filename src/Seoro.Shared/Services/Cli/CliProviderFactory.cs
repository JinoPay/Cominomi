using Microsoft.Extensions.Logging;

namespace Seoro.Shared.Services.Cli;

/// <summary>
///     등록된 <see cref="ICliProvider" /> 목록에서 세션에 맞는 구현체를 선택한다.
/// </summary>
public class CliProviderFactory(
    IEnumerable<ICliProvider> providers,
    ILogger<CliProviderFactory> logger)
    : ICliProviderFactory
{
    private readonly IReadOnlyDictionary<string, ICliProvider> _providers =
        providers.ToDictionary(p => p.ProviderId, StringComparer.OrdinalIgnoreCase);

    public ICliProvider GetProvider(string providerId)
    {
        if (_providers.TryGetValue(providerId, out var provider))
            return provider;

        logger.LogWarning("프로바이더 '{ProviderId}'를 찾을 수 없습니다. Claude로 폴백합니다.", providerId);
        return _providers["claude"];
    }

    public ICliProvider GetProviderForSession(Session session)
        => GetProvider(session.Provider);

    public IReadOnlyList<ICliProvider> GetAllProviders()
        => [.. _providers.Values];
}
