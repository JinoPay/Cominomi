using System.Text;

namespace Seoro.Shared.Services.Chat;

public static class PlanSessionTransferBuilder
{
    private const int MaxRecentMessages = 8;
    private const int MaxCharsPerMessage = 1200;
    private const int MaxPromptChars = 12000;

    public static string Build(Session sourceSession, string userPrompt)
    {
        Guard.NotNull(sourceSession, nameof(sourceSession));
        Guard.NotNullOrWhiteSpace(userPrompt, nameof(userPrompt));

        var sb = new StringBuilder();
        sb.AppendLine("이전 Codex 세션에서 조사한 내용을 이어받아 새 플랜 세션을 시작합니다.");
        sb.AppendLine();
        sb.AppendLine("원본 세션 정보:");
        sb.AppendLine($"- 제목: {sourceSession.Title}");
        sb.AppendLine($"- 세션 ID: {sourceSession.Id}");

        if (!string.IsNullOrWhiteSpace(sourceSession.ConversationId))
            sb.AppendLine($"- Codex 대화 ID: {sourceSession.ConversationId}");

        if (!string.IsNullOrWhiteSpace(sourceSession.Git.WorktreePath))
            sb.AppendLine($"- 작업 경로: {sourceSession.Git.WorktreePath}");

        var recentMessages = sourceSession.GetMessagesSnapshot()
            .Where(m => m.Role is MessageRole.User or MessageRole.Assistant)
            .Where(m => !string.IsNullOrWhiteSpace(m.Text))
            .TakeLast(MaxRecentMessages)
            .ToList();

        if (recentMessages.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("최근 대화 발췌:");

            foreach (var message in recentMessages)
            {
                var role = message.Role == MessageRole.User ? "사용자" : "어시스턴트";
                sb.AppendLine($"[{role}]");
                sb.AppendLine(TrimForPrompt(message.Text, MaxCharsPerMessage));
                sb.AppendLine();
            }
        }

        sb.AppendLine("새 요청:");
        sb.AppendLine(userPrompt.Trim());
        sb.AppendLine();
        sb.Append("위 조사 내용을 바탕으로 코드 수정 없이 계획만 진행하세요. ");
        sb.Append("필요하면 `.claude/plans/` 아래에 구현 계획을 정리하고, 변경 대상 파일과 검증 방법을 구체적으로 적어주세요.");

        return TrimForPrompt(sb.ToString(), MaxPromptChars);
    }

    private static string TrimForPrompt(string text, int maxChars)
    {
        var trimmed = text.Trim();
        if (trimmed.Length <= maxChars)
            return trimmed;

        return $"{trimmed[..(maxChars - 1)]}…";
    }
}
