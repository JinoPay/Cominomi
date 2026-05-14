using Seoro.Shared.Models;
using Seoro.Shared.Services.Chat;

namespace Seoro.Shared.Tests;

public class MessageQueueServiceTests
{
    [Fact]
    public void Enqueue_AddsMessageAndRaisesEvent()
    {
        var svc = new MessageQueueService();
        var raised = new List<string>();
        svc.OnQueueChanged += id => raised.Add(id);

        var msg = svc.Enqueue("s1", new ChatInputMessage { Text = "hello" });

        Assert.Equal(1, svc.Count("s1"));
        Assert.Single(raised);
        Assert.Equal("s1", raised[0]);
        Assert.NotEmpty(msg.Id);
    }

    [Fact]
    public void TryDequeue_ReturnsFifoOrder()
    {
        var svc = new MessageQueueService();
        svc.Enqueue("s1", new ChatInputMessage { Text = "first" });
        svc.Enqueue("s1", new ChatInputMessage { Text = "second" });
        svc.Enqueue("s1", new ChatInputMessage { Text = "third" });

        Assert.True(svc.TryDequeue("s1", out var m1));
        Assert.Equal("first", m1!.Input.Text);
        Assert.True(svc.TryDequeue("s1", out var m2));
        Assert.Equal("second", m2!.Input.Text);
        Assert.True(svc.TryDequeue("s1", out var m3));
        Assert.Equal("third", m3!.Input.Text);
        Assert.False(svc.TryDequeue("s1", out _));
    }

    [Fact]
    public void TryDequeue_EmptySession_ReturnsFalse()
    {
        var svc = new MessageQueueService();
        Assert.False(svc.TryDequeue("missing", out var m));
        Assert.Null(m);
    }

    [Fact]
    public void Remove_ByIdRaisesEventOnlyWhenFound()
    {
        var svc = new MessageQueueService();
        var queued = svc.Enqueue("s1", new ChatInputMessage { Text = "a" });
        svc.Enqueue("s1", new ChatInputMessage { Text = "b" });

        var raised = 0;
        svc.OnQueueChanged += _ => raised++;

        Assert.True(svc.Remove("s1", queued.Id));
        Assert.Equal(1, svc.Count("s1"));
        Assert.Equal(1, raised);

        Assert.False(svc.Remove("s1", "non-existent"));
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Clear_EmptiesQueueAndRaisesEventOnce()
    {
        var svc = new MessageQueueService();
        svc.Enqueue("s1", new ChatInputMessage { Text = "a" });
        svc.Enqueue("s1", new ChatInputMessage { Text = "b" });
        svc.Enqueue("s1", new ChatInputMessage { Text = "c" });

        var raised = 0;
        svc.OnQueueChanged += _ => raised++;

        svc.Clear("s1");

        Assert.Equal(0, svc.Count("s1"));
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Clear_AlreadyEmpty_DoesNotRaiseEvent()
    {
        var svc = new MessageQueueService();
        svc.Enqueue("s1", new ChatInputMessage { Text = "a" });
        svc.Clear("s1");

        var raised = 0;
        svc.OnQueueChanged += _ => raised++;

        svc.Clear("s1");
        Assert.Equal(0, raised);

        svc.Clear("never-existed");
        Assert.Equal(0, raised);
    }

    [Fact]
    public void Sessions_AreIsolated()
    {
        var svc = new MessageQueueService();
        svc.Enqueue("a", new ChatInputMessage { Text = "msg-a-1" });
        svc.Enqueue("a", new ChatInputMessage { Text = "msg-a-2" });
        svc.Enqueue("b", new ChatInputMessage { Text = "msg-b-1" });

        Assert.Equal(2, svc.Count("a"));
        Assert.Equal(1, svc.Count("b"));
        Assert.Empty(svc.GetQueue("c"));

        svc.Clear("a");
        Assert.Equal(0, svc.Count("a"));
        Assert.Equal(1, svc.Count("b"));
    }

    [Fact]
    public void GetQueue_ReturnsSnapshotNotLiveReference()
    {
        var svc = new MessageQueueService();
        svc.Enqueue("s1", new ChatInputMessage { Text = "a" });

        var snapshot = svc.GetQueue("s1");
        Assert.Single(snapshot);

        svc.Enqueue("s1", new ChatInputMessage { Text = "b" });
        // 스냅샷은 원본과 분리되어야 함
        Assert.Single(snapshot);
        Assert.Equal(2, svc.GetQueue("s1").Count);
    }

    [Fact]
    public void QueuedMessage_PreviewText_TruncatesLongInput()
    {
        var longText = new string('a', 200);
        var msg = new QueuedMessage { Input = new ChatInputMessage { Text = longText } };

        Assert.True(msg.PreviewText.Length <= 40);
        Assert.EndsWith("…", msg.PreviewText);
    }

    [Fact]
    public void QueuedMessage_PreviewText_AttachmentsOnly()
    {
        var msg = new QueuedMessage
        {
            Input = new ChatInputMessage
            {
                Text = string.Empty,
                Attachments = [new PendingAttachment { FileName = "a.txt" }, new PendingAttachment { FileName = "b.txt" }]
            }
        };

        Assert.Equal("[첨부 2]", msg.PreviewText);
    }
}
