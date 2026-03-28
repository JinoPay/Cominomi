namespace Cominomi.Shared.Models;

public class SessionReplaySummary
{
    public required string Id { get; init; }
    public required string FilePath { get; init; }
    public string? ProjectPath { get; set; }
    public int EntryCount { get; set; }
    public int MessageCount { get; set; }
    public int ToolCallCount { get; set; }
    public DateTime? FirstTimestamp { get; set; }
    public DateTime? LastTimestamp { get; set; }
    public string? FirstMessage { get; set; }

    public TimeSpan? Duration => FirstTimestamp != null && LastTimestamp != null
        ? LastTimestamp.Value - FirstTimestamp.Value
        : null;
}

public class SessionReplayEvent
{
    public required string Type { get; init; } // "user", "assistant", "tool_use", "tool_result"
    public DateTime? Timestamp { get; set; }
    public string Content { get; set; } = "";
    public string? ToolName { get; set; }
    public List<ToolCallInfo>? ToolCalls { get; set; }
}

public class ToolCallInfo
{
    public string Name { get; set; } = string.Empty;
    public string InputPreview { get; set; } = string.Empty;
}

public class SessionSearchResult
{
    public string SessionId { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
}

public class SessionTagStore
{
    public Dictionary<string, List<string>> Tags { get; set; } = new();
    public Dictionary<string, string> Notes { get; set; } = new();
}

public class LiveSessionInfo
{
    public string Path { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public int ModifiedSecsAgo { get; set; }
}
