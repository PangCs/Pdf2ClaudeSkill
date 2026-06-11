using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Logging;

public sealed class ActivityLog : IActivityLog
{
    private readonly List<LogEntry> _entries = new();
    private readonly object _lock = new();
    private readonly string _logsDir;

    public event EventHandler<LogEntry>? EntryAdded;

    public ActivityLog() : this(DefaultLogsDir()) { }

    internal ActivityLog(string logsDir)
    {
        _logsDir = logsDir;
    }

    public IReadOnlyList<LogEntry> Entries
    {
        get { lock (_lock) return _entries.ToList(); }
    }

    public void Append(string message)
    {
        var entry = new LogEntry(DateTimeOffset.Now, message);
        lock (_lock)
        {
            _entries.Add(entry);
            WriteToFile(entry);
        }
        EntryAdded?.Invoke(this, entry);
    }

    private void WriteToFile(LogEntry entry)
    {
        try
        {
            Directory.CreateDirectory(_logsDir);
            var filePath = Path.Combine(_logsDir, $"Pdf2ClaudeSkill_{entry.Timestamp:yyyy-MM-dd}.log");
            File.AppendAllText(filePath, $"[{entry.Timestamp:HH:mm:ss zzz}] {entry.Message}{Environment.NewLine}");
        }
        catch
        {
            // file logging is best-effort; never crash the app
        }
    }

    private static string DefaultLogsDir() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pdf2ClaudeSkill",
            "Logs");
}
