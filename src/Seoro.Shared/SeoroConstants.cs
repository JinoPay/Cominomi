namespace Seoro.Shared;

/// <summary>
///     Shared constants used across multiple services and models.
/// </summary>
public static class SeoroConstants
{
    // Session limits
    public const int MaxActiveSessionsPerWorkspace = 20;
    public const int MaxContextItemTokens = 2_000; // single note/todo/plan file

    // System prompt size limits (token-based via TokenEstimator)
    public const int MaxContextPromptTokens = 5_000; // notes + todos + plans combined
    public const int MaxMemoryEntryTokens = 1_000; // single memory entry content
    public const int MaxMemoryPromptTokens = 2_500; // all memory entries combined
    public const int MaxSystemPromptTokens = 10_000; // overall system prompt budget
    public const string BranchPrefix = "seoro/";

    public const string DefaultEffortLevel = "auto";

    // Chat input: paste text longer than this threshold is auto-converted to a file attachment
    public const int PasteAsFileThreshold = 500;

    // Default values duplicated across AppSettings, Session, ClaudeService, etc.
    public const string DefaultPermissionMode = "bypassAll";
    public const string PathCaptureSentinel = "___SEORO_PATH___";
    public const string RequiredClaudeVersion = "2.1.81";

    // Persistent worktree directory constraint — always injected for worktree sessions
    public const string SystemInstructionWorktreeDir =
        "작업 디렉터리 제약 (절대 위반 금지): 이 세션은 git worktree에서 실행됩니다. " +
        "모든 파일 읽기·쓰기·검색은 반드시 워크트리 디렉터리({0}) 내에서 수행하세요. " +
        "원본 저장소 경로나 워크트리 외부 경로에서 절대 파일을 읽거나 수정하지 마세요. " +
        "사용자가 명시적으로 지정한 경로가 아닌 한, 워크트리 외부로 나가지 마세요.";

    // Built-in system instructions injected until session title is determined
    public static string GetSystemInstructionWorktree(string language = "en")
    {
        var langRule = language == "en"
            ? "use lowercase-kebab-case English words"
            : "한글 또는 영문 소문자 kebab-case";

        return "필수 최우선 실행 (Plan·Explore 모드 포함 모든 상황에서 최우선 — 절대 생략·지연 금지):\n" +
               "`git branch -m seoro/<설명적-이름>`을 즉시 실행하여 현재 브랜치 이름을 변경하세요.\n" +
               "현재 브랜치 이름을 확인하지 마세요. 이 단계를 건너뛰거나 미루지 마세요.\n" +
               $"브랜치 이름 규칙: 구체적이고 간결하게 (30자 이내), \"seoro/\" 접두사, {langRule}.";
    }

    public static string GetSystemInstructionLocalDir(string language = "en")
    {
        var langRule = language == "en"
            ? "Title must be in English."
            : "제목은 한국어로 작성하세요.";

        return "필수 최우선 실행 (Plan·Explore 모드 포함 모든 상황에서 최우선 — 절대 생략·지연 금지):\n" +
               "이 세션은 로컬 디렉터리를 사용하므로 브랜치 이름을 변경하지 마세요.\n" +
               "대신 대화 내용에 맞는 작업 제목을 정하여 첫 응답에 반드시 포함하세요:\n" +
               "<!-- seoro:title 제목 -->\n" +
               "이 마커를 절대 생략하지 마세요.\n" +
               $"제목 규칙: 구체적이고 간결하게 (30자 이내). {langRule}";
    }

    public const string TitleMarkerPrefix = "<!-- seoro:title ";
    public const string TitleMarkerSuffix = " -->";
    public const string TruncationMarker = "\n\n[...truncated, {0:N0} tokens total]";
    public static readonly TimeSpan ShellCacheTtl = TimeSpan.FromMinutes(10);

    // Timeout / retry constants
    public static readonly TimeSpan WhichTimeout = TimeSpan.FromSeconds(5);

    // Environment variables shared by multiple process-launching services
    public static class Env
    {
        /// <summary>
        ///     Common environment block that suppresses interactive prompts and color codes.
        ///     Used by GitService, ClaudeCliResolver, etc.
        /// </summary>
        public static readonly Dictionary<string, string> NoColorEnv = new()
        {
            [NoColor] = "1"
        };

        public static readonly Dictionary<string, string> GitEnv = new()
        {
            [GitTerminalPrompt] = "0",
            [NoColor] = "1"
        };


        public const string GitTerminalPrompt = "GIT_TERMINAL_PROMPT";
        public const string HookEvent = "SEORO_HOOK_EVENT";
        public const string NoColor = "NO_COLOR";
    }
}