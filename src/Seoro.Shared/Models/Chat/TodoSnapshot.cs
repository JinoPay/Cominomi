namespace Seoro.Shared.Models.Chat;

public enum TodoStatus
{
    Pending,
    InProgress,
    Completed
}

public sealed record TodoEntry(string Content, string ActiveForm, TodoStatus Status);

public sealed class TodoSnapshot
{
    public IReadOnlyList<TodoEntry> Entries { get; init; } = Array.Empty<TodoEntry>();
    public DateTime UpdatedAt { get; init; }

    public int Completed => Entries.Count(e => e.Status == TodoStatus.Completed);
    public int Total => Entries.Count;
    public bool HasInProgress => Entries.Any(e => e.Status == TodoStatus.InProgress);
    public bool AllDone => Total > 0 && Completed == Total;
}

public enum TodoFloaterVisibility
{
    Hidden,
    Chip,
    Expanded
}
