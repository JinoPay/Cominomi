using Cominomi.Shared.Models;

namespace Cominomi.Shared.Services;

public interface IMcpService
{
    Task<McpOperationResult> AddServerAsync(McpServer server);
    Task<McpOperationResult> RemoveServerAsync(string name, string scope);
    Task<McpOperationResult> UpdateServerAsync(string oldName, string oldScope, McpServer server);
    Task<List<McpServer>> ListServersAsync(string? projectPath = null);
    Task<McpOperationResult> ImportFromClaudeDesktopAsync(string scope = "user");
    Task<McpOperationResult> ImportFromJsonAsync(string json, string scope);
    Task<McpServerStatus> TestConnectionAsync(McpServer server, CancellationToken ct = default);
}
