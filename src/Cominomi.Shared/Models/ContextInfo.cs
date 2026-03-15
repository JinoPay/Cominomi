namespace Cominomi.Shared.Models;

public class ContextInfo
{
    public string Notes { get; set; } = string.Empty;
    public string Todos { get; set; } = string.Empty;
    public List<PlanFile> Plans { get; set; } = [];
}

public class PlanFile
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}
