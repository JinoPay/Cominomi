using Seoro.Shared.Models;

namespace Seoro.Shared.Services;

public interface IFilePickerService
{
    Task<List<PendingAttachment>?> PickFilesAsync();
}