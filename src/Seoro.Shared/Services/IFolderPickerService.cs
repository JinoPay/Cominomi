namespace Seoro.Shared.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}