using Cominomi.Shared.Services;

namespace Cominomi.Desktop.Services;

public class WindowCloseGuardService : IWindowCloseGuardService
{
    public void ForceClose() => Environment.Exit(0);
}
