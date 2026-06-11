namespace PdfExtractToSkill.Application.Interfaces;

public interface IActivityLog
{
    void Append(string message);
    IReadOnlyList<LogEntry> Entries { get; }
    event EventHandler<LogEntry> EntryAdded;
}
