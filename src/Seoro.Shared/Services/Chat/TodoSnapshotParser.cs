using System.Text.Json;

namespace Seoro.Shared.Services.Chat;

public static class TodoSnapshotParser
{
    public static bool IsTodoWriteTool(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name.Equals("TodoWrite", StringComparison.OrdinalIgnoreCase)
            || name.Equals("todo_write", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParse(string? toolInput, out TodoSnapshot snapshot)
    {
        snapshot = new TodoSnapshot();
        if (string.IsNullOrWhiteSpace(toolInput)) return false;

        try
        {
            using var doc = JsonDocument.Parse(toolInput);
            if (!doc.RootElement.TryGetProperty("todos", out var todosEl) ||
                todosEl.ValueKind != JsonValueKind.Array)
                return false;

            var entries = new List<TodoEntry>(todosEl.GetArrayLength());
            foreach (var item in todosEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                var content = item.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                var activeForm = item.TryGetProperty("activeForm", out var a) ? a.GetString() ?? "" : "";
                var statusStr = item.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";

                if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(activeForm))
                    continue;

                entries.Add(new TodoEntry(content, activeForm, ParseStatus(statusStr)));
            }

            snapshot = new TodoSnapshot
            {
                Entries = entries,
                UpdatedAt = DateTime.UtcNow
            };
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static TodoStatus ParseStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "completed" or "done" => TodoStatus.Completed,
            "in_progress" or "inprogress" or "in-progress" or "running" => TodoStatus.InProgress,
            _ => TodoStatus.Pending
        };
    }
}
