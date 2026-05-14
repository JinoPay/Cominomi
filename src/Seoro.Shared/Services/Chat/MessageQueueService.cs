using System.Collections.Concurrent;
using Seoro.Shared.Models;
using Seoro.Shared.Models.Files;

namespace Seoro.Shared.Services.Chat;

public class MessageQueueService : IMessageQueueService
{
    private readonly ConcurrentDictionary<string, List<QueuedMessage>> _queues = new();

    public event Action<string>? OnQueueChanged;

    public IReadOnlyList<QueuedMessage> GetQueue(string sessionId)
    {
        if (!_queues.TryGetValue(sessionId, out var queue))
            return [];

        lock (queue)
            return queue.ToList();
    }

    public int Count(string sessionId)
    {
        if (!_queues.TryGetValue(sessionId, out var queue))
            return 0;

        lock (queue)
            return queue.Count;
    }

    public QueuedMessage Enqueue(string sessionId, ChatInputMessage input)
    {
        var message = new QueuedMessage { Input = input };
        var queue = _queues.GetOrAdd(sessionId, _ => []);

        lock (queue)
            queue.Add(message);

        OnQueueChanged?.Invoke(sessionId);
        return message;
    }

    public bool TryDequeue(string sessionId, out QueuedMessage? message)
    {
        message = null;
        if (!_queues.TryGetValue(sessionId, out var queue))
            return false;

        lock (queue)
        {
            if (queue.Count == 0)
                return false;

            message = queue[0];
            queue.RemoveAt(0);
        }

        OnQueueChanged?.Invoke(sessionId);
        return true;
    }

    public bool Remove(string sessionId, string messageId)
    {
        if (!_queues.TryGetValue(sessionId, out var queue))
            return false;

        bool removed;
        lock (queue)
            removed = queue.RemoveAll(m => m.Id == messageId) > 0;

        if (removed)
            OnQueueChanged?.Invoke(sessionId);
        return removed;
    }

    public void Clear(string sessionId)
    {
        if (!_queues.TryGetValue(sessionId, out var queue))
            return;

        bool hadItems;
        lock (queue)
        {
            hadItems = queue.Count > 0;
            queue.Clear();
        }

        if (hadItems)
            OnQueueChanged?.Invoke(sessionId);
    }
}
