using Seoro.Shared.Models.Files;

namespace Seoro.Shared.Models;

public class QueuedMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public ChatInputMessage Input { get; init; } = new();
    public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;

    public string PreviewText
    {
        get
        {
            var text = Input.Text;
            if (string.IsNullOrEmpty(text))
                return Input.Attachments.Count > 0 ? $"[첨부 {Input.Attachments.Count}]" : string.Empty;
            return text.Length > 40 ? string.Concat(text.AsSpan(0, 37), "…") : text;
        }
    }
}
