using Seoro.Shared.Services;

namespace Seoro.Desktop.Services;

public class WindowCloseGuardService : IWindowCloseGuardService
{
    public void ForceClose() => Environment.Exit(0);
}
