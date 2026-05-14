namespace Seoro.Shared.Tests;

public class TodoSnapshotParserTests
{
    [Fact]
    public void TryParse_NormalInput_ReturnsAllEntries()
    {
        const string json = """
            {
              "todos": [
                {"content": "Read files", "activeForm": "Reading files", "status": "completed"},
                {"content": "Write models", "activeForm": "Writing models", "status": "in_progress"},
                {"content": "Build", "activeForm": "Building", "status": "pending"}
              ]
            }
            """;

        var ok = TodoSnapshotParser.TryParse(json, out var snap);

        Assert.True(ok);
        Assert.Equal(3, snap.Total);
        Assert.Equal(1, snap.Completed);
        Assert.True(snap.HasInProgress);
        Assert.False(snap.AllDone);
        Assert.Equal(TodoStatus.Completed, snap.Entries[0].Status);
        Assert.Equal(TodoStatus.InProgress, snap.Entries[1].Status);
        Assert.Equal(TodoStatus.Pending, snap.Entries[2].Status);
        Assert.Equal("Writing models", snap.Entries[1].ActiveForm);
        Assert.Equal("Reading files", snap.Entries[0].ActiveForm);
    }

    [Fact]
    public void TryParse_AllCompleted_AllDoneTrue()
    {
        const string json = """
            {"todos":[{"content":"a","activeForm":"a","status":"completed"},
                      {"content":"b","activeForm":"b","status":"completed"}]}
            """;

        Assert.True(TodoSnapshotParser.TryParse(json, out var snap));
        Assert.True(snap.AllDone);
        Assert.False(snap.HasInProgress);
    }

    [Fact]
    public void TryParse_PartialJson_ReturnsFalse()
    {
        const string truncated = """{"todos":[{"content":"Read", "activeForm":"Reading","sta""";

        var ok = TodoSnapshotParser.TryParse(truncated, out var snap);

        Assert.False(ok);
        Assert.Equal(0, snap.Total);
    }

    [Fact]
    public void TryParse_EmptyTodosArray_ReturnsEmpty()
    {
        Assert.True(TodoSnapshotParser.TryParse("""{"todos":[]}""", out var snap));
        Assert.Equal(0, snap.Total);
    }

    [Fact]
    public void TryParse_NoTodosKey_ReturnsFalse()
    {
        Assert.False(TodoSnapshotParser.TryParse("""{"other":1}""", out _));
    }

    [Fact]
    public void TryParse_NullOrWhitespace_ReturnsFalse()
    {
        Assert.False(TodoSnapshotParser.TryParse(null, out _));
        Assert.False(TodoSnapshotParser.TryParse("", out _));
        Assert.False(TodoSnapshotParser.TryParse("   ", out _));
    }

    [Theory]
    [InlineData("completed", TodoStatus.Completed)]
    [InlineData("done", TodoStatus.Completed)]
    [InlineData("in_progress", TodoStatus.InProgress)]
    [InlineData("inprogress", TodoStatus.InProgress)]
    [InlineData("in-progress", TodoStatus.InProgress)]
    [InlineData("running", TodoStatus.InProgress)]
    [InlineData("pending", TodoStatus.Pending)]
    [InlineData("waiting", TodoStatus.Pending)]
    [InlineData("", TodoStatus.Pending)]
    [InlineData("garbage", TodoStatus.Pending)]
    public void TryParse_StatusMapping(string raw, TodoStatus expected)
    {
        var json = $$"""{"todos":[{"content":"x","activeForm":"x","status":"{{raw}}"}]}""";
        Assert.True(TodoSnapshotParser.TryParse(json, out var snap));
        Assert.Equal(expected, snap.Entries[0].Status);
    }

    [Theory]
    [InlineData("TodoWrite", true)]
    [InlineData("todowrite", true)]
    [InlineData("todo_write", true)]
    [InlineData("TODO_WRITE", true)]
    [InlineData("Read", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsTodoWriteTool_AcceptsExpectedNames(string? name, bool expected)
    {
        Assert.Equal(expected, TodoSnapshotParser.IsTodoWriteTool(name));
    }
}
