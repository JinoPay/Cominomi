using Seoro.Shared.Models;
using Seoro.Shared.Services;

namespace Seoro.Shared.Tests;

public class ProcessErrorClassifierTests
{
    // --- Git ---

    [Fact]
    public void ClassifyGitError_Rejected_ReturnsPushRejected()
    {
        var error = ProcessErrorClassifier.ClassifyGitError("! [rejected] main -> main (non-fast-forward)");
        Assert.Equal(ErrorCode.BranchPushRejected, error.Code);
    }

    [Fact]
    public void ClassifyGitError_NotARepo_ReturnsNotAGitRepo()
    {
        var error = ProcessErrorClassifier.ClassifyGitError("fatal: not a git repository");
        Assert.Equal(ErrorCode.NotAGitRepo, error.Code);
    }

    [Fact]
    public void ClassifyGitError_Unknown_ReturnsFallback()
    {
        var error = ProcessErrorClassifier.ClassifyGitError("some unknown error");
        Assert.Equal(ErrorCode.BranchPushFailed, error.Code);
    }

    // --- Claude ---

    [Fact]
    public void ClassifyClaudeError_RequiresVerbose_ReturnsClaudeProcessFailed()
    {
        var error = ProcessErrorClassifier.ClassifyClaudeError("requires --verbose flag");
        Assert.Equal(ErrorCode.ClaudeProcessFailed, error.Code);
    }

    [Fact]
    public void ClassifyClaudeError_ApiOverloaded_ReturnsStreamingFailed()
    {
        var error = ProcessErrorClassifier.ClassifyClaudeError("API error: service overloaded");
        Assert.Equal(ErrorCode.StreamingFailed, error.Code);
    }

    [Fact]
    public void ClassifyClaudeError_AuthError_ReturnsPermanent()
    {
        var error = ProcessErrorClassifier.ClassifyClaudeError("invalid api key provided");
        Assert.Equal(ErrorCode.ClaudeProcessFailed, error.Code);
        Assert.Equal(ErrorCategory.Permanent, error.Category);
    }

    // --- Push/Merge backward compatibility ---

    [Fact]
    public void ClassifyPushError_Rejected_ReturnsPushRejected()
    {
        var error = ProcessErrorClassifier.ClassifyPushError("! [rejected]");
        Assert.Equal(ErrorCode.BranchPushRejected, error.Code);
    }

    [Fact]
    public void ClassifyPushError_Other_ReturnsPushFailed()
    {
        var error = ProcessErrorClassifier.ClassifyPushError("network error");
        Assert.Equal(ErrorCode.BranchPushFailed, error.Code);
    }

    // --- Codex ---

    [Fact]
    public void ClassifyCodexError_ApiKey_ReturnsPermanent()
    {
        var error = ProcessErrorClassifier.ClassifyCodexError("Error: invalid api key");
        Assert.Equal(ErrorCode.CodexProcessFailed, error.Code);
        Assert.Equal(ErrorCategory.Permanent, error.Category);
    }

    [Fact]
    public void ClassifyCodexError_OpenAiApiKey_ReturnsPermanent()
    {
        var error = ProcessErrorClassifier.ClassifyCodexError("OPENAI_API_KEY environment variable not set");
        Assert.Equal(ErrorCode.CodexProcessFailed, error.Code);
        Assert.Equal(ErrorCategory.Permanent, error.Category);
    }

    [Fact]
    public void ClassifyCodexError_RateLimit_ReturnsTransient()
    {
        var error = ProcessErrorClassifier.ClassifyCodexError("Error: 429 too many requests");
        Assert.Equal(ErrorCode.StreamingFailed, error.Code);
        Assert.Equal(ErrorCategory.Transient, error.Category);
    }

    [Fact]
    public void ClassifyCodexError_ModelNotFound_ReturnsPermanent()
    {
        var error = ProcessErrorClassifier.ClassifyCodexError("Error: model not found");
        Assert.Equal(ErrorCode.CodexProcessFailed, error.Code);
        Assert.Equal(ErrorCategory.Permanent, error.Category);
    }

    [Fact]
    public void ClassifyCodexError_SandboxViolation_ReturnsPermanent()
    {
        var error = ProcessErrorClassifier.ClassifyCodexError("sandbox: permission denied for /etc/passwd");
        Assert.Equal(ErrorCode.CodexSandboxViolation, error.Code);
        Assert.Equal(ErrorCategory.Permanent, error.Category);
    }

    [Fact]
    public void ClassifyCodexError_Network_ReturnsTransient()
    {
        var error = ProcessErrorClassifier.ClassifyCodexError("ECONNREFUSED when connecting to api.openai.com");
        Assert.Equal(ErrorCode.StreamingFailed, error.Code);
        Assert.Equal(ErrorCategory.Transient, error.Category);
    }

    [Fact]
    public void ClassifyCodexError_Unknown_FallsBack()
    {
        var error = ProcessErrorClassifier.ClassifyCodexError("some unknown codex error");
        Assert.Equal(ErrorCode.CodexProcessFailed, error.Code);
        Assert.Equal(ErrorCategory.Unknown, error.Category);
    }
}
