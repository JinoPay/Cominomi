using Markdig;

namespace Cominomi.Shared.Helpers;

public static class MarkdownHelper
{
    /// <summary>
    /// Pipeline for chat messages — HTML disabled for security.
    /// </summary>
    public static readonly MarkdownPipeline ChatPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak()
        .DisableHtml()
        .Build();

    /// <summary>
    /// Pipeline for trusted content (plan review, activity summary, file content, agent output).
    /// </summary>
    public static readonly MarkdownPipeline TrustedPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();
}
