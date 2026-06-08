using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Python;

public sealed class PrerequisiteChecker : IPrerequisiteChecker
{
    private readonly IPythonDetector _detector;
    private readonly IPythonRunner _runner;

    public PrerequisiteChecker(IPythonDetector detector, IPythonRunner runner)
    {
        _detector = detector;
        _runner = runner;
    }

    public PrerequisiteReport CheckAll()
    {
        var pythonPath = _detector.TryDetect();
        if (pythonPath is null)
            return new PrerequisiteReport(false, null, false, null);

        var importResult = _runner.Run(pythonPath, "-c", ["import pymupdf"]);
        if (importResult.ExitCode == 0)
            return new PrerequisiteReport(true, pythonPath, true, null);

        var installResult = _runner.Run(pythonPath, "-m", ["pip", "install", "pymupdf"]);
        if (installResult.ExitCode == 0)
            return new PrerequisiteReport(true, pythonPath, true, null);

        var error = installResult.Stderr.Trim() is { Length: > 0 } s ? s : installResult.Stdout.Trim();
        return new PrerequisiteReport(true, pythonPath, false, error);
    }
}
