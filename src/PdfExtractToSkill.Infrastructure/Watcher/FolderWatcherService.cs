using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Watcher;

public sealed class FolderWatcherService : IFolderWatcherService, IDisposable
{
    private readonly TimeSpan _debounce;
    private readonly IActivityLog? _log;
    private FileSystemWatcher? _watcher;
    private readonly Dictionary<string, Timer> _timers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public event EventHandler<string>? PdfDetected;

    public FolderWatcherService(IActivityLog log) : this(log, TimeSpan.FromMilliseconds(500)) { }

    internal FolderWatcherService(TimeSpan debounce) : this(null, debounce) { }

    private FolderWatcherService(IActivityLog? log, TimeSpan debounce)
    {
        _log = log;
        _debounce = debounce;
    }

    public void Start(string path)
    {
        Stop();
        _watcher = new FileSystemWatcher(path, "*.pdf")
        {
            NotifyFilter = NotifyFilters.FileName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
        };
        _watcher.Created += OnCreated;
    }

    public void Stop()
    {
        if (_watcher is null)
            return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnCreated;
        _watcher.Dispose();
        _watcher = null;

        lock (_lock)
        {
            foreach (var t in _timers.Values)
                t.Dispose();
            _timers.Clear();
        }
    }

    public void Dispose() => Stop();

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            if (_timers.TryGetValue(e.FullPath, out var existing))
            {
                existing.Change(_debounce, Timeout.InfiniteTimeSpan);
                return;
            }

            var timer = new Timer(FireEvent, e.FullPath, _debounce, Timeout.InfiniteTimeSpan);
            _timers[e.FullPath] = timer;
        }
    }

    private void FireEvent(object? state)
    {
        var path = (string)state!;
        lock (_lock)
        {
            if (_timers.TryGetValue(path, out var t))
            {
                t.Dispose();
                _timers.Remove(path);
            }
        }
        _log?.Append($"PDF detected: {path}");
        PdfDetected?.Invoke(this, path);
    }
}
