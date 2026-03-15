namespace Cominomi.Shared.Models;

public class SkillDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PromptTemplate { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; } = true;
}
