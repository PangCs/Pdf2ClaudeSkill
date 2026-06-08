using System.Collections.Concurrent;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Watcher;

public sealed class PdfQueue : IPdfQueue
{
    private readonly ConcurrentQueue<string> _queue = new();
    private volatile bool _paused;

    public int Count => _queue.Count;

    public void Enqueue(string path) => _queue.Enqueue(path);

    public string? TryDequeue()
    {
        if (_paused)
            return null;
        return _queue.TryDequeue(out var path) ? path : null;
    }

    public void Pause() => _paused = true;
    public void Resume() => _paused = false;
}
