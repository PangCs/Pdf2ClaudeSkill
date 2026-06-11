using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Orchestration;

public sealed class ExtractionOrchestrator : IExtractionOrchestrator
{
    private readonly IPythonRunner _runner;
    private readonly ISkillInstaller _installer;
    private readonly IActivityLog? _log;
    private readonly string _scriptPath;

    public ExtractionOrchestrator(IPythonRunner runner, ISkillInstaller installer, IActivityLog log)
        : this(runner, installer, log, ResolveScriptPath()) { }

    internal ExtractionOrchestrator(IPythonRunner runner, ISkillInstaller installer, string scriptPath)
        : this(runner, installer, null, scriptPath) { }

    private ExtractionOrchestrator(IPythonRunner runner, ISkillInstaller installer, IActivityLog? log, string scriptPath)
    {
        _runner = runner;
        _installer = installer;
        _log = log;
        _scriptPath = scriptPath;
    }

    public ExtractionResult Extract(ExtractionRequest request)
    {
        if (!request.Overwrite && _installer.Exists(request.SkillName))
            return new ExtractionResult(false, request.SkillName, null,
                $"Skill '{request.SkillName}' already exists. Set Overwrite=true to replace it.");

        var args = new[]
        {
            "--pdf", request.PdfPath,
            "--output-dir", request.OutputPath,
            "--skill-name", request.SkillName,
            "--skill-description", request.Description,
        };

        _log?.Append($"Running: {request.PythonExePath} {_scriptPath} --pdf \"{request.PdfPath}\" --output-dir \"{request.OutputPath}\" --skill-name {request.SkillName}");

        var result = _runner.Run(request.PythonExePath, _scriptPath, args);
        if (result.ExitCode != 0)
        {
            var error = result.Stderr.Trim() is { Length: > 0 } s ? s : result.Stdout.Trim();
            _log?.Append($"Extraction failed: {error}");
            return new ExtractionResult(false, request.SkillName, null, error);
        }

        var outputFilePath = Path.Combine(request.OutputPath, Path.GetFileNameWithoutExtension(request.PdfPath) + ".md");
        var definition = new SkillDefinition(
            Name: request.SkillName,
            Description: request.Description,
            OutputFilePath: outputFilePath,
            SourceFileName: Path.GetFileName(request.PdfPath));

        _installer.Install(definition);
        _log?.Append($"Skill installed: {request.SkillName} → {outputFilePath}");

        return new ExtractionResult(true, request.SkillName, outputFilePath, null);
    }

    private static string ResolveScriptPath() =>
        Path.Combine(AppContext.BaseDirectory, "extract.py");
}
