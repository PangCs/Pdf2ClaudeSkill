using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Python;

public sealed class PythonDetector : IPythonDetector
{
    private static readonly string[] Candidates = ["py", "python", "python3"];

    private readonly IEnumerable<string> _pathDirs;
    private readonly Func<string, bool> _fileExists;

    public PythonDetector()
        : this(
            (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator),
            File.Exists)
    { }

    internal PythonDetector(IEnumerable<string> pathDirs, Func<string, bool> fileExists)
    {
        _pathDirs = pathDirs;
        _fileExists = fileExists;
    }

    public string? TryDetect()
    {
        foreach (var candidate in Candidates)
        {
            var found = FindExecutable(candidate);
            if (found is not null)
                return found;
        }
        return null;
    }

    private string? FindExecutable(string name)
    {
        foreach (var dir in _pathDirs)
        {
            var full = Path.Combine(dir.Trim(), name + ".exe");
            if (_fileExists(full))
                return full;
        }
        return null;
    }
}
