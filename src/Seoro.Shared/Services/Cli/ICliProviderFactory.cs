namespace Seoro.Shared.Services.Cli;

/// <summary>
///     세션에 맞는 <see cref="ICliProvider" />를 반환하는 팩토리.
/// </summary>
public interface ICliProviderFactory
{
    /// <summary>프로바이더 ID로 구현체를 반환한다.</summary>
    ICliProvider GetProvider(string providerId);

    /// <summary>세션의 Provider 필드에 맞는 구현체를 반환한다.</summary>
    ICliProvider GetProviderForSession(Session session);

    /// <summary>현재 설치된(탐지 가능한) 프로바이더 목록을 반환한다.</summary>
    IReadOnlyList<ICliProvider> GetAllProviders();
}
