using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Seoro.Shared.Services.Codex;

/// <summary>
///     Codex CLI 바이너리를 탐색하고 실행 경로를 해결한다.
///     ClaudeCliResolver와 동일한 패턴으로 구현되며, "codex" 바이너리를 대상으로 한다.
/// </summary>
public class CodexCliResolver(IShellService shellService, IProcessRunner processRunner, ILogger logger)
{
    private readonly SemaphoreSlim _resolveLock = new(1, 1);
    private (string fileName, string argPrefix)? _resolvedCommand;
    private string? _resolvedCommandPath;

    /// <summary>
    ///     Codex CLI가 실제로 존재할 때만 반환한다. 없으면 null.
    /// </summary>
    public async Task<(string fileName, string argPrefix)?> DetectAsync(string? configuredPath)
        => await FindCodexCommandAsync(configuredPath);

    /// <summary>
    ///     Codex CLI 경로를 반환한다. 없으면 bare "codex" 폴백.
    /// </summary>
    public async Task<(string fileName, string argPrefix)> ResolveAsync(string? configuredPath)
    {
        await _resolveLock.WaitAsync();
        try
        {
            if (_resolvedCommand.HasValue && _resolvedCommandPath == configuredPath)
                return _resolvedCommand.Value;

            var result = await FindCodexCommandAsync(configuredPath)
                         ?? (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                             ? ("cmd.exe", "/c codex ")
                             : ("codex", ""));
            _resolvedCommand = result;
            _resolvedCommandPath = configuredPath;
            return result;
        }
        finally
        {
            _resolveLock.Release();
        }
    }

    public async Task<string?> RunSimpleCommandAsync(string fileName, string arguments)
    {
        logger.LogDebug("실행 중: {FileName} {Arguments}", fileName, arguments);
        try
        {
            var args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var loginPath = await shellService.GetLoginShellPathAsync();
            var envVars = new Dictionary<string, string>(SeoroConstants.Env.NoColorEnv);
            if (loginPath != null)
                envVars["PATH"] = loginPath;

            var result = await processRunner.RunAsync(new ProcessRunOptions
            {
                FileName = fileName,
                Arguments = args,
                EnvironmentVariables = envVars,
                Timeout = TimeSpan.FromSeconds(10)
            });
            return result.Stdout;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "간단한 명령 실행 실패: {FileName} {Args}", fileName, arguments);
            return null;
        }
    }

    private static (string fileName, string argPrefix) ResolveWindowsCommand(string resolvedPath)
    {
        var ext = Path.GetExtension(resolvedPath);

        if (ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
            return (resolvedPath, "");

        if (ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bat", StringComparison.OrdinalIgnoreCase))
            return ("cmd.exe", $"/c \"{resolvedPath}\" ");

        var cmdSibling = resolvedPath + ".cmd";
        if (File.Exists(cmdSibling))
            return ("cmd.exe", $"/c \"{cmdSibling}\" ");

        return ("cmd.exe", $"/c \"{resolvedPath}\" ");
    }

    private async Task<(string fileName, string argPrefix)?> FindCodexCommandAsync(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ResolveWindowsCommand(configuredPath);
            return (configuredPath, "");
        }

        var resolved = await shellService.WhichAsync("codex");
        if (resolved != null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ResolveWindowsCommand(resolved);
            return (resolved, "");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string[] windowsCandidates =
            [
                Path.Combine(appData, "npm", "codex.cmd")
            ];

            foreach (var candidate in windowsCandidates)
                if (File.Exists(candidate))
                    return ResolveWindowsCommand(candidate);
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] candidates =
            [
                Path.Combine(home, ".local", "bin", "codex"),
                Path.Combine(home, ".local", "share", "mise", "shims", "codex"), // mise
                Path.Combine(home, ".volta", "bin", "codex"), // volta
                "/opt/homebrew/bin/codex", // Apple Silicon Homebrew
                "/usr/local/bin/codex", // Intel Homebrew
                Path.Combine(home, ".npm", "bin", "codex") // npm global
            ];

            foreach (var candidate in candidates)
                if (File.Exists(candidate))
                    return (candidate, "");
        }

        return null;
    }
}
