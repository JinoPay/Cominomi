using Seoro.Shared.Services.Chat;

namespace Seoro.Shared.Tests;

public class PlanSessionTransferBuilderTests
{
    [Fact]
    public void Build_IncludesSourceMetadataAndRequest()
    {
        var session = new Session
        {
            Id = "source-1",
            Title = "기존 조사 세션",
            ConversationId = "thread-123",
            Git = { WorktreePath = "/repo" },
            Messages =
            [
                new ChatMessage { Role = MessageRole.User, Text = "라우팅 구조를 먼저 조사해줘." },
                new ChatMessage { Role = MessageRole.Assistant, Text = "ChatView와 Orchestrator를 확인했다." }
            ]
        };

        var result = PlanSessionTransferBuilder.Build(session, "이제 수정 계획만 세워줘.");

        Assert.Contains("기존 조사 세션", result);
        Assert.Contains("thread-123", result);
        Assert.Contains("라우팅 구조를 먼저 조사해줘.", result);
        Assert.Contains("ChatView와 Orchestrator를 확인했다.", result);
        Assert.Contains("이제 수정 계획만 세워줘.", result);
    }

    [Fact]
    public void Build_TruncatesLongMessages()
    {
        var longText = new string('a', 2000);
        var session = new Session
        {
            Messages =
            [
                new ChatMessage { Role = MessageRole.User, Text = longText }
            ]
        };

        var result = PlanSessionTransferBuilder.Build(session, "플랜");

        Assert.DoesNotContain(longText, result);
        Assert.Contains("…", result);
    }
}
