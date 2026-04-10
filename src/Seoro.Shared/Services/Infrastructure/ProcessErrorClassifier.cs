
namespace Seoro.Shared.Services.Infrastructure;

/// <summary>
///     외부 프로세스(git, gh, claude)의 stderr/오류 텍스트를 분류합니다.
///     GitService, GhService, ClaudeService에 산재된 임시 Contains() 검사를 대체합니다.
/// </summary>
public static class ProcessErrorClassifier
{
    private static readonly ErrorPattern[] ClaudePatterns =
    [
        new(["requires --verbose"], ErrorCode.ClaudeProcessFailed, ErrorCategory.Transient),
        new(["API error", "overloaded"], ErrorCode.StreamingFailed, ErrorCategory.Transient),
        new(["authentication", "unauthorized", "invalid api key"], ErrorCode.ClaudeProcessFailed,
            ErrorCategory.Permanent)
    ];

    private static readonly ErrorPattern[] CodexPatterns =
    [
        new(["invalid api key", "OPENAI_API_KEY", "authentication failed", "unauthorized"],
            ErrorCode.CodexProcessFailed, ErrorCategory.Permanent),
        new(["rate limit", "429", "too many requests"],
            ErrorCode.StreamingFailed, ErrorCategory.Transient, IsRateLimit: true),
        new(["model not found", "does not exist", "invalid model"],
            ErrorCode.CodexProcessFailed, ErrorCategory.Permanent),
        new(["sandbox", "permission denied", "not allowed", "blocked by policy"],
            ErrorCode.CodexSandboxViolation, ErrorCategory.Permanent),
        new(["ECONNREFUSED", "network error", "connection timeout"],
            ErrorCode.StreamingFailed, ErrorCategory.Transient)
    ];
    // ─── 패턴 정의 ────────────────────────────────────────

    private static readonly ErrorPattern[] GitPatterns =
    [
        new(["rejected"], ErrorCode.BranchPushRejected, ErrorCategory.Transient),
        new(["not a git repository"], ErrorCode.NotAGitRepo, ErrorCategory.Permanent),
        new(["fatal: unable to create", "worktree"], ErrorCode.WorktreeCreationFailed, ErrorCategory.Permanent)
    ];

    /// <summary>
    ///     Codex CLI 오류를 분류합니다.
    /// </summary>
    public static AppError ClassifyCodexError(string stderr, string? stdout = null)
    {
        var combined = CombineText(stderr, stdout);
        return MatchPatterns(combined, stderr, CodexPatterns)
               ?? new AppError(ErrorCode.CodexProcessFailed, ErrorCategory.Unknown, stderr);
    }

    /// <summary>
    ///     Claude CLI 오류를 분류합니다.
    /// </summary>
    public static AppError ClassifyClaudeError(string stderr, string? stdout = null)
    {
        var combined = CombineText(stderr, stdout);
        return MatchPatterns(combined, stderr, ClaudePatterns)
               ?? new AppError(ErrorCode.ClaudeProcessFailed, ErrorCategory.Unknown, stderr);
    }

    // ─── 공개 API ─────────────────────────────────────────────────

    /// <summary>
    ///     git 프로세스 오류를 분류합니다.
    /// </summary>
    public static AppError ClassifyGitError(string stderr, string? stdout = null)
    {
        var combined = CombineText(stderr, stdout);
        return MatchPatterns(combined, stderr, GitPatterns)
               ?? new AppError(ErrorCode.BranchPushFailed, ErrorCategory.Unknown, stderr);
    }

    /// <summary>
    ///     push 오류를 분류합니다 (AppError.ClassifyPushError와 하위 호환).
    /// </summary>
    public static AppError ClassifyPushError(string errorText)
    {
        if (errorText.Contains("rejected", StringComparison.OrdinalIgnoreCase))
            return AppError.PushRejected(errorText);
        return AppError.PushFailed(errorText);
    }

    private static AppError? MatchPatterns(string combined, string originalError, ErrorPattern[] patterns)
    {
        foreach (var pattern in patterns)
            if (pattern.Keywords.Any(k => combined.Contains(k, StringComparison.OrdinalIgnoreCase)))
                return new AppError(pattern.Code, pattern.Category, originalError);
        return null;
    }

    // ─── 내부 ──────────────────────────────────────────────────

    private static string CombineText(string stderr, string? stdout)
    {
        return string.IsNullOrEmpty(stdout) ? stderr : $"{stderr} {stdout}";
    }

    private sealed record ErrorPattern(
        string[] Keywords,
        ErrorCode Code,
        ErrorCategory Category,
        bool IsRateLimit = false);
}