using Seoro.Shared.Models.Git;

namespace Seoro.Shared.Services.Git;

public static class SideBySideDiffBuilder
{
    public static IReadOnlyList<SideBySideRow> Build(DiffHunk hunk)
    {
        var rows = new List<SideBySideRow>();
        var oldNo = hunk.OldStart;
        var newNo = hunk.NewStart;

        var deletionBuffer = new List<DiffLine>();
        var additionBuffer = new List<DiffLine>();

        void FlushBuffers()
        {
            var paired = Math.Min(deletionBuffer.Count, additionBuffer.Count);
            for (var i = 0; i < paired; i++)
            {
                var del = deletionBuffer[i];
                var add = additionBuffer[i];
                var (oldSeg, newSeg) = WordDiff.Compute(del.Text, add.Text);
                rows.Add(new SideBySideRow
                {
                    Kind = SideBySideRowKind.Modified,
                    OldLineNumber = oldNo++,
                    NewLineNumber = newNo++,
                    OldText = del.Text,
                    NewText = add.Text,
                    OldSegments = oldSeg,
                    NewSegments = newSeg
                });
            }

            for (var i = paired; i < deletionBuffer.Count; i++)
            {
                rows.Add(new SideBySideRow
                {
                    Kind = SideBySideRowKind.DeleteOnly,
                    OldLineNumber = oldNo++,
                    NewLineNumber = null,
                    OldText = deletionBuffer[i].Text,
                    NewText = ""
                });
            }

            for (var i = paired; i < additionBuffer.Count; i++)
            {
                rows.Add(new SideBySideRow
                {
                    Kind = SideBySideRowKind.AddOnly,
                    OldLineNumber = null,
                    NewLineNumber = newNo++,
                    OldText = "",
                    NewText = additionBuffer[i].Text
                });
            }

            deletionBuffer.Clear();
            additionBuffer.Clear();
        }

        foreach (var line in hunk.Lines)
        {
            switch (line.Type)
            {
                case DiffLineType.Deletion:
                    deletionBuffer.Add(line);
                    break;
                case DiffLineType.Addition:
                    additionBuffer.Add(line);
                    break;
                case DiffLineType.Context:
                    FlushBuffers();
                    rows.Add(new SideBySideRow
                    {
                        Kind = SideBySideRowKind.Context,
                        OldLineNumber = oldNo++,
                        NewLineNumber = newNo++,
                        OldText = line.Text,
                        NewText = line.Text
                    });
                    break;
            }
        }

        FlushBuffers();
        return rows;
    }
}
