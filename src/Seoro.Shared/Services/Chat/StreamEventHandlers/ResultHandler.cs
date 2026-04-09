using Microsoft.Extensions.Logging;

namespace Seoro.Shared.Services.Chat.StreamEventHandlers;

public class ResultHandler(IChatState chatState, ILogger<ResultHandler> logger) : IStreamEventHandler
{
    public string EventType => "result";

    public async Task HandleAsync(StreamEvent evt, StreamProcessingContext ctx)
    {
        var session = ctx.Session;

        if (string.IsNullOrEmpty(session.ConversationId) && !string.IsNullOrEmpty(evt.SessionId))
            session.ConversationId = evt.SessionId;

        var resultUsage = evt.Usage ?? evt.Message?.Usage ?? StreamEventUtils.TryExtractUsageFromExtensionData(evt);

        logger.LogDebug("결과 이벤트: Usage={HasUsage}, AccIn={AccIn}, AccOut={AccOut}",
            evt.Usage != null, ctx.AccInputTokens, ctx.AccOutputTokens);

        // 세션 수준의 토큰 수 누적 (세션 내 표시용)
        if (resultUsage != null && !ctx.UsageRecorded)
        {
            session.TotalInputTokens += resultUsage.InputTokens;
            session.TotalOutputTokens += resultUsage.OutputTokens;
            ctx.UsageRecorded = true;
        }
        else if (!ctx.UsageRecorded && (ctx.AccInputTokens > 0 || ctx.AccOutputTokens > 0))
        {
            session.TotalInputTokens += ctx.AccInputTokens;
            session.TotalOutputTokens += ctx.AccOutputTokens;
            ctx.UsageRecorded = true;
            logger.LogDebug("누적된 델타에서 사용량. In={In}, Out={Out}",
                ctx.AccInputTokens, ctx.AccOutputTokens);
        }

        // 대기 중인 토큰 정리 — 커밋된 값은 이제 TotalInputTokens/TotalOutputTokens에 반영됨
        session.PendingInputTokens = 0;
        session.PendingOutputTokens = 0;

        // 폴백: Parts가 비어있으면 결과 콘텐츠에서 채우기
        if (ctx.AssistantMessage.Parts.Count == 0)
        {
            if (evt.Message?.Content != null)
                foreach (var block in evt.Message.Content)
                {
                    if (block.Type == "text" && block.Text != null)
                        chatState.AppendText(ctx.AssistantMessage, block.Text);
                    if (block.Type == "tool_use" && block.Name == "ExitPlanMode")
                        ctx.ExitPlanModeDetected = true;
                }

            if (string.IsNullOrEmpty(ctx.AssistantMessage.Text) && !string.IsNullOrEmpty(evt.Result))
                chatState.AppendText(ctx.AssistantMessage, evt.Result);
        }
        else
        {
            // Parts가 있어도 ExitPlanMode 확인
            if (evt.Message?.Content != null)
                foreach (var block in evt.Message.Content)
                    if (block.Type == "tool_use" && block.Name == "ExitPlanMode")
                        ctx.ExitPlanModeDetected = true;
        }

        await Task.CompletedTask;
    }
}