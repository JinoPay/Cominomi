namespace Seoro.Shared.Tests;

public class ChatStateTodoTests
{
    private static ChatState NewState() =>
        new(new ActiveSessionRegistry(), new FakeBus());

    private static TodoSnapshot Snap(int total, int completed = 0)
    {
        var entries = new List<TodoEntry>();
        for (var i = 0; i < total; i++)
        {
            var status = i < completed ? TodoStatus.Completed : TodoStatus.Pending;
            entries.Add(new TodoEntry($"task {i}", $"doing task {i}", status));
        }
        return new TodoSnapshot { Entries = entries, UpdatedAt = DateTime.UtcNow };
    }

    [Fact]
    public void UpdateTodoSnapshot_PromotesFromHiddenToChip()
    {
        var s = NewState();
        Assert.Equal(TodoFloaterVisibility.Hidden, s.TodoFloaterState);

        s.UpdateTodoSnapshot(Snap(3));

        Assert.NotNull(s.CurrentTodos);
        Assert.Equal(3, s.CurrentTodos!.Total);
        Assert.Equal(TodoFloaterVisibility.Chip, s.TodoFloaterState);
    }

    [Fact]
    public void UpdateTodoSnapshot_KeepsExpandedWhenAlreadyExpanded()
    {
        var s = NewState();
        s.UpdateTodoSnapshot(Snap(2));
        s.SetTodoFloaterState(TodoFloaterVisibility.Expanded);

        s.UpdateTodoSnapshot(Snap(2, completed: 1));

        Assert.Equal(TodoFloaterVisibility.Expanded, s.TodoFloaterState);
        Assert.Equal(1, s.CurrentTodos!.Completed);
    }

    [Fact]
    public void Dismiss_HidesFloater_NextUpdateReshowsAsChip()
    {
        var s = NewState();
        s.UpdateTodoSnapshot(Snap(2));
        s.DismissTodoFloater();
        Assert.Equal(TodoFloaterVisibility.Hidden, s.TodoFloaterState);

        s.UpdateTodoSnapshot(Snap(2, completed: 1));

        Assert.Equal(TodoFloaterVisibility.Chip, s.TodoFloaterState);
    }

    [Fact]
    public void SetSession_DifferentId_ResetsTodos()
    {
        var s = NewState();
        s.SetSession(new Session { Id = "s1" });
        s.UpdateTodoSnapshot(Snap(3));
        Assert.NotNull(s.CurrentTodos);

        s.SetSession(new Session { Id = "s2" });

        Assert.Null(s.CurrentTodos);
        Assert.Equal(TodoFloaterVisibility.Hidden, s.TodoFloaterState);
    }

    [Fact]
    public void SetSession_SameId_KeepsTodos()
    {
        var s = NewState();
        var session = new Session { Id = "s1" };
        s.SetSession(session);
        s.UpdateTodoSnapshot(Snap(2));

        s.SetSession(session);

        Assert.NotNull(s.CurrentTodos);
        Assert.Equal(TodoFloaterVisibility.Chip, s.TodoFloaterState);
    }

    private sealed class FakeBus : IChatEventBus
    {
        public event Action? OnAny;
        public void Publish<T>(T evt) where T : ChatEvent => OnAny?.Invoke();
        public IDisposable Subscribe<T>(Action<T> handler) where T : ChatEvent => new Noop();
        private sealed class Noop : IDisposable { public void Dispose() { } }
    }
}
