namespace Cominomi.Shared.Services;

/// <summary>
/// Writes files atomically: write to temp file first, then rename to target.
/// Prevents data corruption from crashes or concurrent writes mid-write.
/// </summary>
public static class AtomicFileWriter
{
    public static async Task WriteAsync(string targetPath, string content)
    {
        var dir = Path.GetDirectoryName(targetPath);
        if (dir != null)
            Directory.CreateDirectory(dir);

        var tmpPath = targetPath + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tmpPath, content);
            File.Move(tmpPath, targetPath, overwrite: true);
        }
        finally
        {
            // Clean up temp file if move failed
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
        }
    }
}
