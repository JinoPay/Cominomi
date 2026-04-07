using Seoro.Shared.Models;

namespace Seoro.Shared.Services;

public interface ISystemPromptBuilder
{
    Task<string?> BuildAsync(Session session, Workspace? workspace);
}