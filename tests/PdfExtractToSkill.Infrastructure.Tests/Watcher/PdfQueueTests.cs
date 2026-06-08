using PdfExtractToSkill.Infrastructure.Watcher;

namespace PdfExtractToSkill.Infrastructure.Tests.Watcher;

public class PdfQueueTests
{
    [Fact]
    public void TryDequeue_EmptyQueue_ReturnsNull()
    {
        Assert.Null(new PdfQueue().TryDequeue());
    }

    [Fact]
    public void Enqueue_ThenDequeue_ReturnsSamePath()
    {
        var q = new PdfQueue();
        q.Enqueue(@"C:\docs\file.pdf");

        Assert.Equal(@"C:\docs\file.pdf", q.TryDequeue());
    }

    [Fact]
    public void Dequeue_IsFifo()
    {
        var q = new PdfQueue();
        q.Enqueue("a.pdf");
        q.Enqueue("b.pdf");
        q.Enqueue("c.pdf");

        Assert.Equal("a.pdf", q.TryDequeue());
        Assert.Equal("b.pdf", q.TryDequeue());
        Assert.Equal("c.pdf", q.TryDequeue());
    }

    [Fact]
    public void TryDequeue_AfterAllDequeued_ReturnsNull()
    {
        var q = new PdfQueue();
        q.Enqueue("x.pdf");
        q.TryDequeue();

        Assert.Null(q.TryDequeue());
    }

    [Fact]
    public void Count_ReflectsQueueDepth()
    {
        var q = new PdfQueue();
        Assert.Equal(0, q.Count);
        q.Enqueue("a.pdf");
        Assert.Equal(1, q.Count);
        q.Enqueue("b.pdf");
        Assert.Equal(2, q.Count);
        q.TryDequeue();
        Assert.Equal(1, q.Count);
    }

    [Fact]
    public void Pause_BlocksDequeue()
    {
        var q = new PdfQueue();
        q.Enqueue("a.pdf");
        q.Pause();

        Assert.Null(q.TryDequeue());
        Assert.Equal(1, q.Count);
    }

    [Fact]
    public void Resume_AllowsDequeueAfterPause()
    {
        var q = new PdfQueue();
        q.Enqueue("a.pdf");
        q.Pause();
        q.Resume();

        Assert.Equal("a.pdf", q.TryDequeue());
    }

    [Fact]
    public void Enqueue_WhilePaused_ItemsAccumulate()
    {
        var q = new PdfQueue();
        q.Pause();
        q.Enqueue("a.pdf");
        q.Enqueue("b.pdf");

        Assert.Equal(2, q.Count);
        Assert.Null(q.TryDequeue());
    }

    [Fact]
    public void ThreadSafety_ConcurrentEnqueueDequeue()
    {
        var q = new PdfQueue();
        var enqueued = new System.Collections.Concurrent.ConcurrentBag<string>();
        var dequeued = new System.Collections.Concurrent.ConcurrentBag<string>();

        var writers = Enumerable.Range(0, 10)
            .Select(i => Task.Run(() =>
            {
                var p = $"file{i}.pdf";
                q.Enqueue(p);
                enqueued.Add(p);
            }));
        var readers = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < 5; i++)
                {
                    var p = q.TryDequeue();
                    if (p is not null) dequeued.Add(p);
                    Thread.SpinWait(100);
                }
            }));

        Task.WaitAll([.. writers, .. readers]);

        // drain remaining
        string? remaining;
        while ((remaining = q.TryDequeue()) is not null)
            dequeued.Add(remaining);

        Assert.Equal(enqueued.OrderBy(x => x), dequeued.OrderBy(x => x));
    }
}
