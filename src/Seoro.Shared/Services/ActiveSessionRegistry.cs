using System.Collections.Concurrent;
using Seoro.Shared.Models;

namespace Seoro.Shared.Services;

public class ActiveSessionRegistry : IActiveSessionRegistry
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public Session? Get(string sessionId)
    {
        return _sessions.GetValueOrDefault(sessionId);
    }

    public void Register(Session session)
    {
        _sessions[session.Id] = session;
    }

    public void Unregister(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}