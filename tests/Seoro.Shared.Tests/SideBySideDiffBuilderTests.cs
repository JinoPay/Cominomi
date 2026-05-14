using Seoro.Shared.Models.Git;
using Seoro.Shared.Services.Git;

namespace Seoro.Shared.Tests;

public class SideBySideDiffBuilderTests
{
    private static DiffHunk Hunk(int oldStart, int newStart, params (DiffLineType type, string text)[] lines)
    {
        return new DiffHunk
        {
            OldStart = oldStart,
            NewStart = newStart,
            Lines = lines.Select(l => new DiffLine { Type = l.type, Text = l.text }).ToList()
        };
    }

    [Fact]
    public void Build_OnlyAdditions_ProducesAddOnlyRows()
    {
        var hunk = Hunk(10, 10,
            (DiffLineType.Context, "a"),
            (DiffLineType.Addition, "b"),
            (DiffLineType.Addition, "c"));

        var rows = SideBySideDiffBuilder.Build(hunk);

        Assert.Equal(3, rows.Count);
        Assert.Equal(SideBySideRowKind.Context, rows[0].Kind);
        Assert.Equal(SideBySideRowKind.AddOnly, rows[1].Kind);
        Assert.Equal(SideBySideRowKind.AddOnly, rows[2].Kind);
        Assert.Null(rows[1].OldLineNumber);
        Assert.Equal(11, rows[1].NewLineNumber);
        Assert.Equal(12, rows[2].NewLineNumber);
    }

    [Fact]
    public void Build_OnlyDeletions_ProducesDeleteOnlyRows()
    {
        var hunk = Hunk(5, 5,
            (DiffLineType.Deletion, "x"),
            (DiffLineType.Deletion, "y"));

        var rows = SideBySideDiffBuilder.Build(hunk);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(SideBySideRowKind.DeleteOnly, r.Kind));
        Assert.Equal(5, rows[0].OldLineNumber);
        Assert.Equal(6, rows[1].OldLineNumber);
        Assert.Null(rows[0].NewLineNumber);
    }

    [Fact]
    public void Build_EqualDeletionAdditionCount_ProducesModifiedRowsWithWordDiff()
    {
        var hunk = Hunk(1, 1,
            (DiffLineType.Deletion, "hello world"),
            (DiffLineType.Deletion, "foo bar"),
            (DiffLineType.Addition, "hello mars"),
            (DiffLineType.Addition, "foo baz"));

        var rows = SideBySideDiffBuilder.Build(hunk);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(SideBySideRowKind.Modified, r.Kind));
        Assert.NotNull(rows[0].OldSegments);
        Assert.NotNull(rows[0].NewSegments);
        Assert.Equal("hello world", rows[0].OldText);
        Assert.Equal("hello mars", rows[0].NewText);
        Assert.Equal(1, rows[0].OldLineNumber);
        Assert.Equal(1, rows[0].NewLineNumber);
        Assert.Equal(2, rows[1].OldLineNumber);
        Assert.Equal(2, rows[1].NewLineNumber);
    }

    [Fact]
    public void Build_MoreDeletionsThanAdditions_GeneratesPairsThenDeleteOnly()
    {
        var hunk = Hunk(1, 1,
            (DiffLineType.Deletion, "a"),
            (DiffLineType.Deletion, "b"),
            (DiffLineType.Deletion, "c"),
            (DiffLineType.Addition, "A"));

        var rows = SideBySideDiffBuilder.Build(hunk);

        Assert.Equal(3, rows.Count);
        Assert.Equal(SideBySideRowKind.Modified, rows[0].Kind);
        Assert.Equal(SideBySideRowKind.DeleteOnly, rows[1].Kind);
        Assert.Equal(SideBySideRowKind.DeleteOnly, rows[2].Kind);
        Assert.Equal(1, rows[0].OldLineNumber);
        Assert.Equal(1, rows[0].NewLineNumber);
        Assert.Equal(2, rows[1].OldLineNumber);
        Assert.Equal(3, rows[2].OldLineNumber);
    }

    [Fact]
    public void Build_MoreAdditionsThanDeletions_GeneratesPairsThenAddOnly()
    {
        var hunk = Hunk(1, 1,
            (DiffLineType.Deletion, "a"),
            (DiffLineType.Addition, "A"),
            (DiffLineType.Addition, "B"),
            (DiffLineType.Addition, "C"));

        var rows = SideBySideDiffBuilder.Build(hunk);

        Assert.Equal(3, rows.Count);
        Assert.Equal(SideBySideRowKind.Modified, rows[0].Kind);
        Assert.Equal(SideBySideRowKind.AddOnly, rows[1].Kind);
        Assert.Equal(SideBySideRowKind.AddOnly, rows[2].Kind);
        Assert.Equal(2, rows[1].NewLineNumber);
        Assert.Equal(3, rows[2].NewLineNumber);
    }

    [Fact]
    public void Build_ContextLines_IncrementBothCounters()
    {
        var hunk = Hunk(10, 20,
            (DiffLineType.Context, "ctx1"),
            (DiffLineType.Context, "ctx2"),
            (DiffLineType.Addition, "added"));

        var rows = SideBySideDiffBuilder.Build(hunk);

        Assert.Equal(3, rows.Count);
        Assert.Equal(10, rows[0].OldLineNumber);
        Assert.Equal(20, rows[0].NewLineNumber);
        Assert.Equal(11, rows[1].OldLineNumber);
        Assert.Equal(21, rows[1].NewLineNumber);
        Assert.Equal(SideBySideRowKind.AddOnly, rows[2].Kind);
        Assert.Equal(22, rows[2].NewLineNumber);
    }

    [Fact]
    public void Build_DeletionBlockFollowedByContextThenAddition_DoesNotPairAcrossContext()
    {
        var hunk = Hunk(1, 1,
            (DiffLineType.Deletion, "del"),
            (DiffLineType.Context, "ctx"),
            (DiffLineType.Addition, "add"));

        var rows = SideBySideDiffBuilder.Build(hunk);

        Assert.Equal(3, rows.Count);
        Assert.Equal(SideBySideRowKind.DeleteOnly, rows[0].Kind);
        Assert.Equal(SideBySideRowKind.Context, rows[1].Kind);
        Assert.Equal(SideBySideRowKind.AddOnly, rows[2].Kind);
    }
}
