using Cominomi.Shared.Models;

namespace Cominomi.Shared.Services;

public interface IActivityService
{
    Task<List<ActivityDateGroup>> GetWorkspaceActivityAsync(string workspaceId, CancellationToken ct = default);
}
