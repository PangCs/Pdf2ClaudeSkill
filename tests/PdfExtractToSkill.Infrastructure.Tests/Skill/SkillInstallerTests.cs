using PdfExtractToSkill.Application;
using PdfExtractToSkill.Infrastructure.Skill;

namespace PdfExtractToSkill.Infrastructure.Tests.Skill;

public class SkillInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private SkillInstaller Make() => new(_root);

    private static SkillDefinition Sample(string name = "my-skill") => new(
        Name: name,
        Description: "the Rorze EFEM communication spec",
        OutputFilePath: @"C:\output\my-skill\Rorze_EFEM.md",
        SourceFileName: "Rorze_EFEM.pdf");

    // ── Install ──────────────────────────────────────────────────────────────

    [Fact]
    public void Install_CreatesSkillDirectory()
    {
        Make().Install(Sample());

        Assert.True(Directory.Exists(Path.Combine(_root, "my-skill")));
    }

    [Fact]
    public void Install_WritesSkillMdFile()
    {
        Make().Install(Sample());

        Assert.True(File.Exists(Path.Combine(_root, "my-skill", "SKILL.md")));
    }

    [Fact]
    public void Install_SkillMdContainsSkillName()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("my-skill", content);
    }

    [Fact]
    public void Install_SkillMdContainsDescription()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("Rorze EFEM communication spec", content);
    }

    [Fact]
    public void Install_SkillMdDoesNotContainOutputFilePath()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.DoesNotContain(@"C:\output\my-skill\Rorze_EFEM.md", content);
    }

    [Fact]
    public void Install_SkillMdDoesNotContainSourceFileName()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.DoesNotContain("Rorze_EFEM.pdf", content);
    }

    [Fact]
    public void Install_SkillMdEnforcesIdontKnowWorkflow()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("I don't know", content);
        Assert.Contains("not in the document", content);
    }

    [Fact]
    public void Install_SkillMdEnforcesNoEstimatesRule()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("Do not estimate", content);
    }

    [Fact]
    public void Install_SkillMdEnforcesCitationBlock()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("> **Page:**", content);
        Assert.Contains("**Section:**", content);
    }

    [Fact]
    public void Install_SkillMdEnforcesMultiSectionCitation()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("cite ALL", content);
    }

    [Fact]
    public void Install_SkillMdEnforcesNoCitationOmission()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("Do not omit the citation block", content);
    }

    [Fact]
    public void Install_SkillMdBodyIsIdenticalForDifferentDocuments()
    {
        var installer = Make();
        installer.Install(Sample("skill-a") with
        {
            Description = "spec A",
            OutputFilePath = @"C:\docs\spec-a.md",
            SourceFileName = "spec-a.pdf"
        });
        installer.Install(Sample("skill-b") with
        {
            Description = "spec B",
            OutputFilePath = @"C:\docs\spec-b.md",
            SourceFileName = "spec-b.pdf"
        });

        var bodyA = ExtractBody(File.ReadAllText(Path.Combine(_root, "skill-a", "SKILL.md")));
        var bodyB = ExtractBody(File.ReadAllText(Path.Combine(_root, "skill-b", "SKILL.md")));

        Assert.Equal(bodyA, bodyB);
    }

    [Fact]
    public void Install_WritesSourcePathMdInsideSkillDirectory()
    {
        Make().Install(Sample());

        Assert.True(File.Exists(Path.Combine(_root, "my-skill", "source-path.md")));
    }

    [Fact]
    public void Install_SourcePathMdContainsExactOutputFilePath()
    {
        Make().Install(Sample());

        var actual = File.ReadAllText(Path.Combine(_root, "my-skill", "source-path.md"));
        Assert.Equal(@"C:\output\my-skill\Rorze_EFEM.md", actual);
    }

    [Fact]
    public void Install_DoesNotWriteLegacyConfigSidecar()
    {
        Make().Install(Sample());

        Assert.False(File.Exists(Path.Combine(_root, "my-skill-config")));
    }

    [Fact]
    public void Install_OverwritesExistingSkill()
    {
        var installer = Make();
        installer.Install(Sample());

        var updated = Sample() with { Description = "updated description" };
        installer.Install(updated);

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("updated description", content);
    }

    // ── Exists ───────────────────────────────────────────────────────────────

    [Fact]
    public void Exists_ReturnsFalse_WhenNotInstalled()
    {
        Assert.False(Make().Exists("my-skill"));
    }

    [Fact]
    public void Exists_ReturnsTrue_AfterInstall()
    {
        var installer = Make();
        installer.Install(Sample());

        Assert.True(installer.Exists("my-skill"));
    }

    [Fact]
    public void Exists_ReturnsFalse_ForDifferentSkillName()
    {
        var installer = Make();
        installer.Install(Sample("skill-a"));

        Assert.False(installer.Exists("skill-b"));
    }

    [Fact]
    public void Exists_ReturnsFalse_WhenSourcePathMdMissing()
    {
        var installer = Make();
        installer.Install(Sample());
        File.Delete(Path.Combine(_root, "my-skill", "source-path.md"));

        Assert.False(installer.Exists("my-skill"));
    }

    // ── Template loading ─────────────────────────────────────────────────────

    [Fact]
    public void Install_UsesBundledTemplateByDefault()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("Do not estimate", content);
        Assert.Contains("> **Page:**", content);
    }

    [Fact]
    public void Install_CustomTemplateOverrideIsUsedWhenProvided()
    {
        var templatePath = Path.Combine(_root, "custom-template.md");
        File.WriteAllText(templatePath, "name: {{name}}\ndesc: {{description}}");

        new SkillInstaller(_root, templatePath).Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Equal("name: my-skill\ndesc: the Rorze EFEM communication spec", content);
    }

    [Fact]
    public void Install_OverrideTemplateTakesPrecedenceOverBundled()
    {
        var overridePath = Path.Combine(_root, "override.md");
        File.WriteAllText(overridePath, "OVERRIDE:{{name}}");

        new SkillInstaller(_root, overridePath).Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.StartsWith("OVERRIDE:", content);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string ExtractBody(string skillMd)
    {
        // Strip YAML frontmatter; normalize line endings so cross-platform
        // comparisons are consistent regardless of WriteAllText behaviour.
        var normalized = skillMd.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n');
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd() == "---")
                return string.Join('\n', lines.Skip(i + 1));
        }
        return normalized;
    }
}
