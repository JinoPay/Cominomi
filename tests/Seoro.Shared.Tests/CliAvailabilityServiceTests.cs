using Microsoft.Extensions.Logging.Abstractions;
using Seoro.Shared.Services.Cli;

namespace Seoro.Shared.Tests;

public class CliAvailabilityServiceTests
{
    [Fact]
    public async Task IsAvailableAsync_ReturnsTrueWhenDetected()
    {
        var provider = new FakeCliProvider("codex", detected: true);
        var factory = new CliProviderFactory([provider], NullLogger<CliProviderFactory>.Instance);
        var service = new CliAvailabilityService(factory, NullLogger<CliAvailabilityService>.Instance);

        var result = await service.IsAvailableAsync("codex");

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsFalseWhenNotDetected()
    {
        var provider = new FakeCliProvider("codex", detected: false);
        var factory = new CliProviderFactory([provider], NullLogger<CliProviderFactory>.Instance);
        var service = new CliAvailabilityService(factory, NullLogger<CliAvailabilityService>.Instance);

        var result = await service.IsAvailableAsync("codex");

        Assert.False(result);
    }

    [Fact]
    public async Task IsAvailableAsync_CachesResult()
    {
        var provider = new FakeCliProvider("codex", detected: true);
        var factory = new CliProviderFactory([provider], NullLogger<CliProviderFactory>.Instance);
        var service = new CliAvailabilityService(factory, NullLogger<CliAvailabilityService>.Instance);

        await service.IsAvailableAsync("codex");
        await service.IsAvailableAsync("codex");

        // DetectCliAsync는 한 번만 호출되어야 함
        Assert.Equal(1, provider.DetectCallCount);
    }

    [Fact]
    public async Task InvalidateAsync_ClearsCache()
    {
        var provider = new FakeCliProvider("codex", detected: true);
        var factory = new CliProviderFactory([provider], NullLogger<CliProviderFactory>.Instance);
        var service = new CliAvailabilityService(factory, NullLogger<CliAvailabilityService>.Instance);

        await service.IsAvailableAsync("codex");
        await service.InvalidateAsync();

        // 캐시 무효화 후 재감지: 총 2번 호출
        Assert.Equal(2, provider.DetectCallCount);
    }

    [Fact]
    public async Task InvalidateAsync_FiresEvent()
    {
        var provider = new FakeCliProvider("codex", detected: true);
        var factory = new CliProviderFactory([provider], NullLogger<CliProviderFactory>.Instance);
        var service = new CliAvailabilityService(factory, NullLogger<CliAvailabilityService>.Instance);

        var eventFired = false;
        service.OnAvailabilityChanged += () => eventFired = true;

        await service.InvalidateAsync();

        Assert.True(eventFired);
    }

    [Fact]
    public async Task IsAvailableAsync_UnknownProvider_ReturnsFalse()
    {
        var provider = new FakeCliProvider("claude", detected: true);
        var factory = new CliProviderFactory([provider], NullLogger<CliProviderFactory>.Instance);
        var service = new CliAvailabilityService(factory, NullLogger<CliAvailabilityService>.Instance);

        var result = await service.IsAvailableAsync("unknown");

        Assert.False(result);
    }

    private sealed class FakeCliProvider(string id, bool detected) : ICliProvider
    {
        public int DetectCallCount { get; private set; }
        public string ProviderId => id;
        public string DisplayName => id;
        public ProviderCapabilities Capabilities { get; } = new();

        public IAsyncEnumerable<StreamEvent> SendMessageAsync(CliSendOptions options, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<(bool found, string resolvedPath)> DetectCliAsync()
        {
            DetectCallCount++;
            return Task.FromResult((detected, $"/usr/bin/{id}"));
        }

        public Task<string?> GetDetectedVersionAsync()
            => Task.FromResult<string?>("1.0.0");

        public void Cancel(string? sessionId = null) { }
        public void Dispose() { }
    }
}
