using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Seoro.Shared.Components.Layout;

public class LoggingErrorBoundary : ErrorBoundary
{
    [Inject] private ILogger<LoggingErrorBoundary> Logger { get; set; } = null!;

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "Blazor 처리되지 않은 렌더링 예외");
        return base.OnErrorAsync(exception);
    }
}