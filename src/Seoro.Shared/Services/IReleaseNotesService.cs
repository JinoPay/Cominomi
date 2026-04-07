using Seoro.Shared.Models;

namespace Seoro.Shared.Services;

public interface IReleaseNotesService
{
    Task<IReadOnlyList<ReleaseNote>> GetReleaseNotesAsync();
}