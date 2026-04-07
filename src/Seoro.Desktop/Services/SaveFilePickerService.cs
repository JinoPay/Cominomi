using Seoro.Shared.Services;

namespace Seoro.Desktop.Services;

public class SaveFilePickerService(PhotinoWindowHolder windowHolder) : ISaveFilePickerService
{
    public async Task<string?> PickSaveFileAsync(string title, string defaultFileName, string filterName, string filterExtension)
    {
        var window = windowHolder.Window;
        if (window == null) return null;

        return await window.ShowSaveFileAsync(title, defaultFileName);
    }

    public async Task<string?> PickOpenFileAsync(string title, string filterName, string filterExtension)
    {
        var window = windowHolder.Window;
        if (window == null) return null;

        var paths = await window.ShowOpenFileAsync(title, multiSelect: false);
        return paths?.FirstOrDefault();
    }
}
