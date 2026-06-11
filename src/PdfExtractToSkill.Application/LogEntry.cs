namespace PdfExtractToSkill.Application;

public sealed record LogEntry(DateTimeOffset Timestamp, string Message);
