using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;
using PdfExtractToSkill.Infrastructure.Python;

namespace PdfExtractToSkill.Infrastructure.Tests.Python;

public class PrerequisiteCheckerTests
{
    // ── Stubs ────────────────────────────────────────────────────────────────

    private sealed class StubDetector(string? path) : IPythonDetector
    {
        public string? TryDetect() => path;
    }

    private sealed class StubRunner(Func<string, string, string[], ProcessResult> handler) : IPythonRunner
    {
        public ProcessResult Run(string executablePath, string scriptPath, string[] args)
            => handler(executablePath, scriptPath, args);
    }

    private static StubRunner ConstantRunner(ProcessResult result) =>
        new StubRunner((_, _, _) => result);

    // ── Unit tests ───────────────────────────────────────────────────────────

    [Fact]
    public void CheckAll_WhenPythonNotFound_ReturnsPythonNotFound()
    {
        var checker = new PrerequisiteChecker(
            new StubDetector(null),
            ConstantRunner(new ProcessResult(0, "", "")));

        var report = checker.CheckAll();

        Assert.False(report.PythonFound);
        Assert.Null(report.PythonPath);
        Assert.False(report.PyMuPdfInstalled);
        Assert.Null(report.PyMuPdfInstallError);
    }

    [Fact]
    public void CheckAll_WhenPyMuPdfAlreadyImportable_ReturnsInstalledWithNoError()
    {
        var checker = new PrerequisiteChecker(
            new StubDetector(@"C:\Python\python.exe"),
            ConstantRunner(new ProcessResult(0, "", "")));

        var report = checker.CheckAll();

        Assert.True(report.PythonFound);
        Assert.Equal(@"C:\Python\python.exe", report.PythonPath);
        Assert.True(report.PyMuPdfInstalled);
        Assert.Null(report.PyMuPdfInstallError);
    }

    [Fact]
    public void CheckAll_WhenImportFails_AttemptsInstall_AndReturnsInstalledOnSuccess()
    {
        var calls = new List<(string scriptPath, string[] args)>();
        var runner = new StubRunner((_, scriptPath, args) =>
        {
            calls.Add((scriptPath, args));
            return calls.Count == 1
                ? new ProcessResult(1, "", "No module named pymupdf")
                : new ProcessResult(0, "Successfully installed pymupdf", "");
        });

        var checker = new PrerequisiteChecker(new StubDetector(@"C:\Python\python.exe"), runner);
        var report = checker.CheckAll();

        Assert.True(report.PyMuPdfInstalled);
        Assert.Null(report.PyMuPdfInstallError);
        Assert.Equal(2, calls.Count);
        Assert.Equal("-c", calls[0].scriptPath);
        Assert.Equal("-m", calls[1].scriptPath);
        Assert.Equal(["pip", "install", "pymupdf"], calls[1].args);
    }

    [Fact]
    public void CheckAll_WhenImportAndInstallBothFail_ReturnsNotInstalledWithError()
    {
        var runner = new StubRunner((_, scriptPath, _) => scriptPath switch
        {
            "-c" => new ProcessResult(1, "", "No module named pymupdf"),
            "-m" => new ProcessResult(1, "", "pip install failed: network error"),
            _ => new ProcessResult(0, "", ""),
        });

        var checker = new PrerequisiteChecker(new StubDetector(@"C:\Python\python.exe"), runner);
        var report = checker.CheckAll();

        Assert.True(report.PythonFound);
        Assert.False(report.PyMuPdfInstalled);
        Assert.Contains("pip install failed", report.PyMuPdfInstallError);
    }

    [Fact]
    public void CheckAll_WhenInstallFailsWithOnlyStdout_UsesStdoutAsError()
    {
        var runner = new StubRunner((_, scriptPath, _) => scriptPath switch
        {
            "-c" => new ProcessResult(1, "", ""),
            "-m" => new ProcessResult(1, "ERROR: could not find a version", ""),
            _ => new ProcessResult(0, "", ""),
        });

        var checker = new PrerequisiteChecker(new StubDetector(@"C:\Python\python.exe"), runner);
        var report = checker.CheckAll();

        Assert.False(report.PyMuPdfInstalled);
        Assert.Contains("ERROR", report.PyMuPdfInstallError);
    }

    [Fact]
    public void CheckAll_UsesPythonPathFromDetector()
    {
        string? usedPath = null;
        var runner = new StubRunner((exe, _, _) => { usedPath = exe; return new ProcessResult(0, "", ""); });

        var checker = new PrerequisiteChecker(new StubDetector(@"C:\Custom\python.exe"), runner);
        checker.CheckAll();

        Assert.Equal(@"C:\Custom\python.exe", usedPath);
    }

    // ── Integration tests ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Integration")]
    public void CheckAll_Integration_FindsPythonAndReportsPrerequisites()
    {
        var checker = new PrerequisiteChecker(new PythonDetector(), new PythonRunner());

        var report = checker.CheckAll();

        Assert.True(report.PythonFound, "Python must be available in the test environment");
        Assert.NotNull(report.PythonPath);
        Assert.True(File.Exists(report.PythonPath), $"Python path does not exist: {report.PythonPath}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void CheckAll_Integration_PyMuPdfInstalledInCiEnvironment()
    {
        var checker = new PrerequisiteChecker(new PythonDetector(), new PythonRunner());

        var report = checker.CheckAll();

        Assert.True(report.PyMuPdfInstalled,
            $"pymupdf should be installed in CI. InstallError: {report.PyMuPdfInstallError}");
        Assert.Null(report.PyMuPdfInstallError);
    }
}
