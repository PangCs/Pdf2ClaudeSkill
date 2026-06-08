using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;
using PdfExtractToSkill.Infrastructure.Orchestration;
using PdfExtractToSkill.Infrastructure.Python;
using PdfExtractToSkill.Infrastructure.Skill;

namespace PdfExtractToSkill.Infrastructure.Tests.Orchestration;

public class ExtractionOrchestratorTests : IDisposable
{
    private readonly string _outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _skillsRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        foreach (var dir in new[] { _outputDir, _skillsRoot })
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    // ── Stubs ────────────────────────────────────────────────────────────────

    private sealed class StubRunner(Func<string, string, string[], ProcessResult> fn) : IPythonRunner
    {
        public ProcessResult Run(string exe, string script, string[] args) => fn(exe, script, args);
    }

    private static StubRunner SuccessRunner() =>
        new StubRunner((_, _, _) => new ProcessResult(0, "ok", ""));

    private static StubRunner FailureRunner(string error) =>
        new StubRunner((_, _, _) => new ProcessResult(1, "", error));

    private SkillInstaller MakeInstaller() => new(_skillsRoot);

    private static ExtractionRequest SampleRequest(string skillsRoot, string outputDir) => new(
        PdfPath: @"C:\docs\test.pdf",
        OutputPath: outputDir,
        SkillName: "test-doc",
        Description: "test description",
        PythonExePath: @"C:\Python\python.exe",
        Overwrite: false);

    // ── Unit tests ───────────────────────────────────────────────────────────

    [Fact]
    public void Extract_OnSuccess_ReturnsSuccessResult()
    {
        var orchestrator = new ExtractionOrchestrator(SuccessRunner(), MakeInstaller(), "extract.py");
        var req = SampleRequest(_skillsRoot, _outputDir);

        var result = orchestrator.Extract(req);

        Assert.True(result.Success);
        Assert.Equal("test-doc", result.SkillName);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Extract_OnSuccess_InstallsSkill()
    {
        var installer = MakeInstaller();
        var orchestrator = new ExtractionOrchestrator(SuccessRunner(), installer, "extract.py");
        var req = SampleRequest(_skillsRoot, _outputDir);

        orchestrator.Extract(req);

        Assert.True(installer.Exists("test-doc"));
    }

    [Fact]
    public void Extract_OnSuccess_ReturnsOutputFilePath()
    {
        var orchestrator = new ExtractionOrchestrator(SuccessRunner(), MakeInstaller(), "extract.py");
        var req = SampleRequest(_skillsRoot, _outputDir);

        var result = orchestrator.Extract(req);

        Assert.Equal(Path.Combine(_outputDir, "test.md"), result.OutputFilePath);
    }

    [Fact]
    public void Extract_WhenRunnerFails_ReturnsFailureWithError()
    {
        var orchestrator = new ExtractionOrchestrator(
            FailureRunner("ModuleNotFoundError: pymupdf"),
            MakeInstaller(), "extract.py");
        var req = SampleRequest(_skillsRoot, _outputDir);

        var result = orchestrator.Extract(req);

        Assert.False(result.Success);
        Assert.Contains("pymupdf", result.ErrorMessage);
        Assert.Null(result.OutputFilePath);
    }

    [Fact]
    public void Extract_WhenRunnerFails_DoesNotInstallSkill()
    {
        var installer = MakeInstaller();
        var orchestrator = new ExtractionOrchestrator(FailureRunner("error"), installer, "extract.py");
        var req = SampleRequest(_skillsRoot, _outputDir);

        orchestrator.Extract(req);

        Assert.False(installer.Exists("test-doc"));
    }

    [Fact]
    public void Extract_WhenSkillExistsAndOverwriteFalse_ReturnsFailure()
    {
        var installer = MakeInstaller();
        installer.Install(new SkillDefinition("test-doc", "desc",
            Path.Combine(_outputDir, "test.md"), "test.pdf"));

        var orchestrator = new ExtractionOrchestrator(SuccessRunner(), installer, "extract.py");
        var req = SampleRequest(_skillsRoot, _outputDir) with { Overwrite = false };

        var result = orchestrator.Extract(req);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessage);
    }

    [Fact]
    public void Extract_WhenSkillExistsAndOverwriteTrue_Succeeds()
    {
        var installer = MakeInstaller();
        installer.Install(new SkillDefinition("test-doc", "desc",
            Path.Combine(_outputDir, "test.md"), "test.pdf"));

        var orchestrator = new ExtractionOrchestrator(SuccessRunner(), installer, "extract.py");
        var req = SampleRequest(_skillsRoot, _outputDir) with { Overwrite = true };

        var result = orchestrator.Extract(req);

        Assert.True(result.Success);
    }

    [Fact]
    public void Extract_PassesPdfPathAndSkillArgsToRunner()
    {
        string[]? capturedArgs = null;
        var runner = new StubRunner((_, _, args) => { capturedArgs = args; return new ProcessResult(0, "", ""); });
        var orchestrator = new ExtractionOrchestrator(runner, MakeInstaller(), "extract.py");
        var req = SampleRequest(_skillsRoot, _outputDir);

        orchestrator.Extract(req);

        Assert.Contains("--pdf", capturedArgs!);
        Assert.Contains(@"C:\docs\test.pdf", capturedArgs!);
        Assert.Contains("--skill-name", capturedArgs!);
        Assert.Contains("test-doc", capturedArgs!);
        Assert.Contains("--skill-description", capturedArgs!);
        Assert.Contains("test description", capturedArgs!);
    }

    // ── Integration test ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Integration")]
    public void Extract_Integration_RealPdfProducesMarkdownAndSkill()
    {
        var fixtureDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "fixtures"));
        var pdfPath = Path.Combine(fixtureDir, "test-document.pdf");
        var scriptPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "extract.py"));

        Assert.True(File.Exists(pdfPath), $"Fixture PDF not found: {pdfPath}");
        Assert.True(File.Exists(scriptPath), $"extract.py not found: {scriptPath}");

        var pythonPath = new PythonDetector().TryDetect();
        Assert.NotNull(pythonPath);

        var installer = new SkillInstaller(_skillsRoot);
        var orchestrator = new ExtractionOrchestrator(new PythonRunner(), installer, scriptPath);

        var result = orchestrator.Extract(new ExtractionRequest(
            PdfPath: pdfPath,
            OutputPath: _outputDir,
            SkillName: "test-document",
            Description: "integration test document",
            PythonExePath: pythonPath,
            Overwrite: true));

        Assert.True(result.Success, $"Extraction failed: {result.ErrorMessage}");
        Assert.NotNull(result.OutputFilePath);
        Assert.True(File.Exists(result.OutputFilePath), $"Output MD not found: {result.OutputFilePath}");
        var md = File.ReadAllText(result.OutputFilePath);
        Assert.Contains("skill:", md);
        Assert.True(installer.Exists("test-document"));
    }
}
