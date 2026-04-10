using Microsoft.Extensions.Logging.Abstractions;
using Seoro.Shared.Services.Cli;

namespace Seoro.Shared.Tests;

public class CliProviderFactoryTests
{
    private static ICliProvider MakeProvider(string id) => new FakeCliProvider(id);

    [Fact]
    public void GetProvider_ReturnsCorrectProvider()
    {
        var claude = MakeProvider("claude");
        var codex = MakeProvider("codex");
        var factory = new CliProviderFactory([claude, codex], NullLogger<CliProviderFactory>.Instance);

        Assert.Same(claude, factory.GetProvider("claude"));
        Assert.Same(codex, factory.GetProvider("codex"));
    }

    [Fact]
    public void GetProvider_CaseInsensitive()
    {
        var claude = MakeProvider("claude");
        var factory = new CliProviderFactory([claude], NullLogger<CliProviderFactory>.Instance);

        Assert.Same(claude, factory.GetProvider("CLAUDE"));
        Assert.Same(claude, factory.GetProvider("Claude"));
    }

    [Fact]
    public void GetProvider_UnknownId_FallsBackToClaude()
    {
        var claude = MakeProvider("claude");
        var factory = new CliProviderFactory([claude], NullLogger<CliProviderFactory>.Instance);

        // Should fall back to claude, not throw
        Assert.Same(claude, factory.GetProvider("unknown"));
    }

    [Fact]
    public void GetProviderForSession_UsesSessionProvider()
    {
        var claude = MakeProvider("claude");
        var codex = MakeProvider("codex");
        var factory = new CliProviderFactory([claude, codex], NullLogger<CliProviderFactory>.Instance);

        var claudeSession = new Session { Provider = "claude" };
        var codexSession = new Session { Provider = "codex" };

        Assert.Same(claude, factory.GetProviderForSession(claudeSession));
        Assert.Same(codex, factory.GetProviderForSession(codexSession));
    }

    [Fact]
    public void GetProviderForSession_DefaultsToClaudeWhenProviderUnset()
    {
        var claude = MakeProvider("claude");
        var factory = new CliProviderFactory([claude], NullLogger<CliProviderFactory>.Instance);

        // Session.Provider defaults to "claude"
        var session = new Session();

        Assert.Same(claude, factory.GetProviderForSession(session));
    }

    [Fact]
    public void GetAllProviders_ReturnsAll()
    {
        var claude = MakeProvider("claude");
        var codex = MakeProvider("codex");
        var factory = new CliProviderFactory([claude, codex], NullLogger<CliProviderFactory>.Instance);

        var all = factory.GetAllProviders();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, p => p.ProviderId == "claude");
        Assert.Contains(all, p => p.ProviderId == "codex");
    }

    private sealed class FakeCliProvider(string id) : ICliProvider
    {
        public string ProviderId => id;
        public string DisplayName => id;
        public ProviderCapabilities Capabilities { get; } = new();

        public IAsyncEnumerable<StreamEvent> SendMessageAsync(CliSendOptions options, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<(bool found, string resolvedPath)> DetectCliAsync()
            => Task.FromResult((true, $"/usr/bin/{id}"));

        public Task<string?> GetDetectedVersionAsync()
            => Task.FromResult<string?>("1.0.0");

        public void Cancel(string? sessionId = null) { }
        public void Dispose() { }
    }
}
