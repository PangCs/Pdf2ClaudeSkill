using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Orchestration;

public sealed class ExtractionOrchestrator : IExtractionOrchestrator
{
    private readonly IPythonRunner _runner;
    private readonly ISkillInstaller _installer;
    private readonly string _scriptPath;

    public ExtractionOrchestrator(IPythonRunner runner, ISkillInstaller installer)
        : this(runner, installer, ResolveScriptPath()) { }

    internal ExtractionOrchestrator(IPythonRunner runner, ISkillInstaller installer, string scriptPath)
    {
        _runner = runner;
        _installer = installer;
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

        var result = _runner.Run(request.PythonExePath, _scriptPath, args);
        if (result.ExitCode != 0)
            return new ExtractionResult(false, request.SkillName, null,
                result.Stderr.Trim() is { Length: > 0 } s ? s : result.Stdout.Trim());

        var outputFilePath = Path.Combine(request.OutputPath, Path.GetFileNameWithoutExtension(request.PdfPath) + ".md");
        var definition = new SkillDefinition(
            Name: request.SkillName,
            Description: request.Description,
            OutputFilePath: outputFilePath,
            SourceFileName: Path.GetFileName(request.PdfPath));

        _installer.Install(definition);

        return new ExtractionResult(true, request.SkillName, outputFilePath, null);
    }

    private static string ResolveScriptPath() =>
        Path.Combine(AppContext.BaseDirectory, "extract.py");
}
