using System.Reflection;
using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Skill;

public sealed class SkillInstaller : ISkillInstaller
{
    private readonly string _skillsRoot;
    private readonly string? _templatePathOverride;

    public SkillInstaller()
        : this(DefaultSkillsRoot()) { }

    internal SkillInstaller(string skillsRoot, string? templatePathOverride = null)
    {
        _skillsRoot = skillsRoot;
        _templatePathOverride = templatePathOverride;
    }

    public void Install(SkillDefinition definition)
    {
        var skillDir = SkillDir(definition.Name);
        Directory.CreateDirectory(skillDir);

        File.WriteAllText(
            Path.Combine(skillDir, "SKILL.md"),
            RenderTemplate(definition));

        File.WriteAllText(
            Path.Combine(skillDir, "source-path.md"),
            definition.OutputFilePath);
    }

    public bool Exists(string skillName)
    {
        var dir = SkillDir(skillName);
        return File.Exists(Path.Combine(dir, "SKILL.md")) &&
               File.Exists(Path.Combine(dir, "source-path.md"));
    }

    private string SkillDir(string name) => Path.Combine(_skillsRoot, name);

    private static string DefaultSkillsRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "skills");

    private static string UserOverridePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PdfExtractToSkill", "skill-template.md");

    private string RenderTemplate(SkillDefinition def)
    {
        var template = LoadTemplate();
        return template
            .Replace("{{name}}", def.Name)
            .Replace("{{description}}", def.Description);
    }

    private string LoadTemplate()
    {
        if (_templatePathOverride is not null)
            return File.ReadAllText(_templatePathOverride);

        var userOverride = UserOverridePath();
        if (File.Exists(userOverride))
            return File.ReadAllText(userOverride);

        return LoadEmbeddedTemplate();
    }

    private static string LoadEmbeddedTemplate()
    {
        var assembly = typeof(SkillInstaller).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            "PdfExtractToSkill.Infrastructure.Skill.skill-template.md");

        if (stream is null)
            throw new InvalidOperationException(
                "Bundled skill-template.md not found. The Infrastructure assembly may be corrupt.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
