using System.Text.Json.Serialization;

namespace Cominomi.Shared.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HookEvent
{
    OnMessageComplete,
    OnSessionCreate,
    OnSessionArchive,
    OnBranchPush,
    OnPrCreate,
    OnPrMerge
}

public class HookDefinition
{
    public HookEvent Event { get; set; }
    public string Command { get; set; } = string.Empty;
    public string? WorkingDirectory { get; set; }
    public bool Enabled { get; set; } = true;
}
