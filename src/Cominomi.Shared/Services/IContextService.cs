using Cominomi.Shared.Models;

namespace Cominomi.Shared.Services;

public interface IContextService
{
    Task<ContextInfo> LoadContextAsync(string worktreePath);
    Task SaveNotesAsync(string worktreePath, string content);
    Task SaveTodosAsync(string worktreePath, string content);
    Task SavePlanAsync(string worktreePath, string planName, string content);
    Task DeletePlanAsync(string worktreePath, string planName);
    Task<List<PlanFile>> GetPlansAsync(string worktreePath);
    Task EnsureContextDirectoryAsync(string worktreePath);
    Task ArchiveContextAsync(string worktreePath, string archivePath);
    string BuildContextPrompt(ContextInfo context);
}
