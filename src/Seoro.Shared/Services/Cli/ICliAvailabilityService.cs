namespace Seoro.Shared.Services.Cli;

/// <summary>
///     CLI 프로바이더 가용성을 캐싱하여 제공하는 서비스.
///     첫 호출 시 감지를 수행하고 이후 캐시에서 반환한다.
/// </summary>
public interface ICliAvailabilityService
{
    /// <summary>특정 프로바이더의 가용 여부를 반환한다 (캐시됨).</summary>
    Task<bool> IsAvailableAsync(string providerId);

    /// <summary>캐시를 무효화하고 모든 프로바이더를 재감지한다.</summary>
    Task InvalidateAsync();

    /// <summary>가용성이 변경되었을 때 발행되는 이벤트.</summary>
    event Action? OnAvailabilityChanged;
}
