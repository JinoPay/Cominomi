using System.Text.Json.Nodes;

namespace Cominomi.Shared.Models;

public class AccountExportEnvelope
{
    public int SchemaVersion { get; set; } = 1;
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    /// <summary>"win32" | "darwin"</summary>
    public string SourcePlatform { get; set; } = "";
    public AccountExportData Account { get; set; } = new();
    /// <summary>원본 .claude.json 내용 (oauthAccount 포함)</summary>
    public JsonObject? Config { get; set; }
    public AccountExportCredentials Credentials { get; set; } = new();
}

public class AccountExportData
{
    public string ProfileName { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public string AccountUuid { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int SwitchCount { get; set; }
    public long TotalActiveSeconds { get; set; }
}

public class AccountExportCredentials
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
