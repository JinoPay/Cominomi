using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cominomi.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Cominomi.Shared.Services;

public class SessionReplayService : ISessionReplayService
{
    private static readonly string ClaudeProjectsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "projects");

    private static readonly string TagsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "cominomi-session-tags.json");

    private readonly ILogger<SessionReplayService> _logger;

    public SessionReplayService(ILogger<SessionReplayService> logger)
    {
        _logger = logger;
    }

    public Task<List<SessionReplaySummary>> ListSessionsAsync()
    {
        var sessions = ScanAllSessions();
        return Task.FromResult(sessions.OrderByDescending(s => s.LastTimestamp).ToList());
    }

    public Task<(List<SessionReplaySummary> Sessions, int Total, bool HasMore)> ListSessionsPagedAsync(int limit, int offset)
    {
        var all = ScanAllSessions().OrderByDescending(s => s.LastTimestamp).ToList();
        var paged = all.Skip(offset).Take(limit).ToList();
        return Task.FromResult((paged, all.Count, offset + paged.Count < all.Count));
    }

    public Task<List<SessionReplayEvent>> LoadEventsAsync(string filePath, int skip = 0, int take = 100)
    {
        var events = new List<SessionReplayEvent>();
        if (!File.Exists(filePath))
            return Task.FromResult(events);

        try
        {
            var lines = File.ReadLines(filePath).Skip(skip).Take(take);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var evt = ParseEvent(line);
                if (evt != null)
                    events.Add(evt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading session events: {Path}", filePath);
        }

        return Task.FromResult(events);
    }

    public Task<List<SessionSearchResult>> SearchSessionsAsync(string query, int maxResults = 20)
    {
        var results = new List<SessionSearchResult>();
        if (!Directory.Exists(ClaudeProjectsDir) || string.IsNullOrWhiteSpace(query))
            return Task.FromResult(results);

        var queryLower = query.ToLowerInvariant();

        foreach (var projectDir in Directory.GetDirectories(ClaudeProjectsDir))
        {
            foreach (var file in Directory.GetFiles(projectDir, "*.jsonl"))
            {
                try
                {
                    var sessionId = Path.GetFileNameWithoutExtension(file);
                    var projectPath = Path.GetFileName(projectDir);

                    foreach (var line in File.ReadLines(file))
                    {
                        if (results.Count >= maxResults) break;
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (!line.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;

                        try
                        {
                            var node = JsonNode.Parse(line);
                            if (node == null) continue;

                            var type = node["type"]?.GetValue<string>() ?? "";
                            var ts = node["timestamp"]?.GetValue<string>();
                            DateTime timestamp = default;
                            if (ts != null) DateTime.TryParse(ts, out timestamp);

                            // Extract snippet
                            var snippet = ExtractSnippet(node, type, queryLower);
                            if (snippet == null) continue;

                            results.Add(new SessionSearchResult
                            {
                                SessionId = sessionId,
                                ProjectPath = projectPath,
                                Snippet = snippet,
                                Timestamp = timestamp,
                                EventType = type
                            });
                        }
                        catch { /* skip malformed */ }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error searching session: {Path}", file);
                }

                if (results.Count >= maxResults) break;
            }
            if (results.Count >= maxResults) break;
        }

        return Task.FromResult(results);
    }

    public async Task<SessionTagStore> GetTagsAsync()
    {
        if (!File.Exists(TagsFilePath))
            return new SessionTagStore();

        try
        {
            var json = await File.ReadAllTextAsync(TagsFilePath);
            return JsonSerializer.Deserialize<SessionTagStore>(json) ?? new SessionTagStore();
        }
        catch
        {
            return new SessionTagStore();
        }
    }

    public async Task SetTagAsync(string sessionId, List<string> tags)
    {
        var store = await GetTagsAsync();
        if (tags.Count == 0)
            store.Tags.Remove(sessionId);
        else
            store.Tags[sessionId] = tags;

        var json = JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
        var dir = Path.GetDirectoryName(TagsFilePath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(TagsFilePath, json);
    }

    public Task<string> ExportMarkdownAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return Task.FromResult("# Session not found");

        var sb = new StringBuilder();
        sb.AppendLine($"# Session Export");
        sb.AppendLine($"**File**: `{Path.GetFileName(filePath)}`");
        sb.AppendLine($"**Exported**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var node = JsonNode.Parse(line);
                if (node == null) continue;

                var type = node["type"]?.GetValue<string>() ?? "";
                var ts = node["timestamp"]?.GetValue<string>();

                switch (type)
                {
                    case "human" or "user":
                    {
                        var text = node["message"]?["content"]?.GetValue<string>()
                                   ?? node["content"]?.ToString() ?? "";
                        sb.AppendLine($"## User ({ts})");
                        sb.AppendLine();
                        sb.AppendLine(text);
                        sb.AppendLine();
                        break;
                    }
                    case "assistant":
                    {
                        sb.AppendLine($"## Assistant ({ts})");
                        sb.AppendLine();
                        if (node["message"]?["content"] is JsonArray arr)
                        {
                            foreach (var block in arr)
                            {
                                var blockType = block?["type"]?.GetValue<string>();
                                if (blockType == "text")
                                {
                                    sb.AppendLine(block?["text"]?.GetValue<string>() ?? "");
                                    sb.AppendLine();
                                }
                                else if (blockType == "tool_use")
                                {
                                    var name = block?["name"]?.GetValue<string>() ?? "tool";
                                    var input = block?["input"]?.ToJsonString() ?? "{}";
                                    sb.AppendLine($"**Tool: {name}**");
                                    sb.AppendLine("```json");
                                    sb.AppendLine(input.Length > 500 ? input[..500] + "..." : input);
                                    sb.AppendLine("```");
                                    sb.AppendLine();
                                }
                            }
                        }
                        else
                        {
                            sb.AppendLine(node["message"]?["content"]?.ToString()
                                          ?? node["content"]?.ToString() ?? "");
                            sb.AppendLine();
                        }
                        break;
                    }
                    case "tool_result":
                    {
                        var content = node["content"]?.ToString() ?? "";
                        if (content.Length > 0)
                        {
                            sb.AppendLine("**Tool Result:**");
                            sb.AppendLine("```");
                            sb.AppendLine(content.Length > 500 ? content[..500] + "..." : content);
                            sb.AppendLine("```");
                            sb.AppendLine();
                        }
                        break;
                    }
                }
            }
            catch { /* skip */ }
        }

        return Task.FromResult(sb.ToString());
    }

    public Task<List<LiveSessionInfo>> DetectLiveSessionsAsync()
    {
        var result = new List<LiveSessionInfo>();
        if (!Directory.Exists(ClaudeProjectsDir))
            return Task.FromResult(result);

        var now = DateTime.UtcNow;
        var threshold = TimeSpan.FromMinutes(5);

        foreach (var projectDir in Directory.GetDirectories(ClaudeProjectsDir))
        {
            foreach (var file in Directory.GetFiles(projectDir, "*.jsonl"))
            {
                try
                {
                    var lastWrite = File.GetLastWriteTimeUtc(file);
                    var age = now - lastWrite;
                    if (age <= threshold)
                    {
                        result.Add(new LiveSessionInfo
                        {
                            Path = file,
                            ProjectPath = Path.GetFileName(projectDir),
                            ModifiedSecsAgo = (int)age.TotalSeconds
                        });
                    }
                }
                catch { /* skip */ }
            }
        }

        return Task.FromResult(result.OrderBy(l => l.ModifiedSecsAgo).ToList());
    }

    private static readonly SemaphoreSlim _scanSemaphore = new(8);

    private List<SessionReplaySummary> ScanAllSessions()
    {
        if (!Directory.Exists(ClaudeProjectsDir))
            return [];

        var files = new List<(string File, string ProjectDir)>();
        foreach (var projectDir in Directory.GetDirectories(ClaudeProjectsDir))
        {
            foreach (var file in Directory.GetFiles(projectDir, "*.jsonl"))
                files.Add((file, projectDir));
        }

        var results = new SessionReplaySummary?[files.Count];
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            try
            {
                results[i] = ScanSessionFile(files[i].File, files[i].ProjectDir);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error scanning session: {Path}", files[i].File);
            }
        });

        return results.Where(s => s != null).ToList()!;
    }

    private static SessionReplaySummary? ScanSessionFile(string filePath, string projectDir)
    {
        int messages = 0, tools = 0, entryCount = 0;
        string? firstMessage = null;
        DateTime? first = null, last = null;

        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            entryCount++;

            try
            {
                var node = JsonNode.Parse(line);
                if (node == null) continue;

                var type = node["type"]?.GetValue<string>();
                var ts = node["timestamp"]?.GetValue<string>();
                if (ts != null && DateTime.TryParse(ts, out var dt))
                {
                    first ??= dt;
                    last = dt;
                }

                if (type is "human" or "user")
                {
                    messages++;
                    firstMessage ??= node["message"]?["content"]?.GetValue<string>()
                                     ?? node["content"]?.ToString();
                }
                else if (type is "assistant")
                {
                    messages++;
                    if (node["message"]?["content"] is JsonArray contentArr)
                    {
                        foreach (var block in contentArr)
                        {
                            if (block?["type"]?.GetValue<string>() == "tool_use")
                                tools++;
                        }
                    }
                }
                else if (type is "tool_use") tools++;
            }
            catch { /* skip malformed lines */ }
        }

        if (entryCount == 0) return null;

        return new SessionReplaySummary
        {
            Id = Path.GetFileNameWithoutExtension(filePath),
            FilePath = filePath,
            ProjectPath = Path.GetFileName(projectDir),
            EntryCount = entryCount,
            MessageCount = messages,
            ToolCallCount = tools,
            FirstTimestamp = first,
            LastTimestamp = last,
            FirstMessage = firstMessage?.Length > 100 ? firstMessage[..100] + "..." : firstMessage
        };
    }

    private static SessionReplayEvent? ParseEvent(string line)
    {
        try
        {
            var node = JsonNode.Parse(line);
            if (node == null) return null;

            var type = node["type"]?.GetValue<string>() ?? "unknown";

            // Skip noise events
            if (type is "file-history-snapshot" or "last-prompt" or "progress")
                return null;

            DateTime? ts = null;
            var tsStr = node["timestamp"]?.GetValue<string>();
            if (tsStr != null && DateTime.TryParse(tsStr, out var dt)) ts = dt;

            // Check if user message is just a tool_result wrapper
            if (type is "human" or "user")
            {
                var msgContent = node["message"]?["content"];
                if (msgContent is JsonArray userArr)
                {
                    var hasText = userArr.Any(item => item?["type"]?.GetValue<string>() == "text");
                    if (!hasText)
                        return null; // tool_result-only wrapper, skip
                }
            }

            // Extract tool calls from assistant messages
            List<ToolCallInfo>? toolCalls = null;
            var content = type switch
            {
                "human" or "user" => ExtractUserText(node),
                "assistant" => ExtractAssistantText(node, out toolCalls),
                "tool_use" => node["name"]?.GetValue<string>() ?? "tool",
                "tool_result" => node["content"]?.ToString() ?? "",
                _ => node.ToJsonString(new JsonSerializerOptions { WriteIndented = false })
            };

            return new SessionReplayEvent
            {
                Type = type,
                Timestamp = ts,
                Content = content,
                ToolName = type == "tool_use" ? node["name"]?.GetValue<string>() : null,
                ToolCalls = toolCalls
            };
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractUserText(JsonNode node)
    {
        var msgContent = node["message"]?["content"];
        if (msgContent is JsonValue)
            return msgContent.GetValue<string>();

        if (msgContent is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item?["type"]?.GetValue<string>() == "text")
                    return item["text"]?.GetValue<string>() ?? "";
            }
        }

        return node["content"]?.ToString() ?? "";
    }

    private static string ExtractAssistantText(JsonNode node, out List<ToolCallInfo>? toolCalls)
    {
        toolCalls = null;
        var msgContent = node["message"]?["content"];

        if (msgContent is not JsonArray arr)
            return node["message"]?["content"]?.ToString() ?? node["content"]?.ToString() ?? "";

        var textParts = new List<string>();
        var calls = new List<ToolCallInfo>();

        foreach (var item in arr)
        {
            var itemType = item?["type"]?.GetValue<string>();
            if (itemType == "text")
            {
                textParts.Add(item!["text"]?.GetValue<string>() ?? "");
            }
            else if (itemType == "tool_use")
            {
                var name = item!["name"]?.GetValue<string>() ?? "tool";
                var input = item["input"]?.ToJsonString() ?? "{}";
                calls.Add(new ToolCallInfo
                {
                    Name = name,
                    InputPreview = input.Length > 200 ? input[..200] + "..." : input
                });
            }
        }

        if (calls.Count > 0)
            toolCalls = calls;

        return string.Join("\n\n", textParts);
    }

    private static string? ExtractSnippet(JsonNode node, string type, string queryLower)
    {
        string? text = null;

        if (type is "human" or "user")
        {
            text = node["message"]?["content"]?.GetValue<string>()
                   ?? node["content"]?.ToString();
        }
        else if (type is "assistant")
        {
            if (node["message"]?["content"] is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item?["type"]?.GetValue<string>() == "text")
                    {
                        var t = item["text"]?.GetValue<string>();
                        if (t?.Contains(queryLower, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            text = t;
                            break;
                        }
                    }
                }
            }
            text ??= node["message"]?["content"]?.ToString();
        }

        if (text == null || !text.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
            return null;

        var idx = text.IndexOf(queryLower, StringComparison.OrdinalIgnoreCase);
        var start = Math.Max(0, idx - 40);
        var end = Math.Min(text.Length, idx + queryLower.Length + 40);
        var snippet = text[start..end];
        if (start > 0) snippet = "..." + snippet;
        if (end < text.Length) snippet += "...";

        return snippet;
    }
}
