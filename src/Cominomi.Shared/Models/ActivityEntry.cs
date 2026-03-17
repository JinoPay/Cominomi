namespace Cominomi.Shared.Models;

public class ActivityEntry
{
    public string CommitHash { get; set; } = "";
    public string ShortHash { get; set; } = "";
    public string Message { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string SessionId { get; set; } = "";
    public string SessionTitle { get; set; } = "";
    public string BranchName { get; set; } = "";
    public SessionStatus SessionStatus { get; set; }
}

public class ActivityDateGroup
{
    public string Label { get; set; } = "";
    public List<ActivityEntry> Entries { get; set; } = [];
}
