using Seoro.Shared.Models;
using Seoro.Shared.Models.Files;

namespace Seoro.Shared.Services.Chat;

public interface IMessageQueueService
{
    event Action<string>? OnQueueChanged;

    IReadOnlyList<QueuedMessage> GetQueue(string sessionId);
    int Count(string sessionId);
    QueuedMessage Enqueue(string sessionId, ChatInputMessage input);
    bool TryDequeue(string sessionId, out QueuedMessage? message);
    bool Remove(string sessionId, string messageId);
    void Clear(string sessionId);
}
