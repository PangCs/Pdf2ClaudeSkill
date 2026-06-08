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
    public void Install_SkillMdContainsOutputFilePath()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains(@"C:\output\my-skill\Rorze_EFEM.md", content);
    }

    [Fact]
    public void Install_SkillMdContainsSourceFileName()
    {
        Make().Install(Sample());

        var content = File.ReadAllText(Path.Combine(_root, "my-skill", "SKILL.md"));
        Assert.Contains("Rorze_EFEM.pdf", content);
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
    public void Install_WritesConfigFileNextToSkillsRoot()
    {
        Make().Install(Sample());

        Assert.True(File.Exists(Path.Combine(_root, "my-skill-config")));
    }

    [Fact]
    public void Install_ConfigFileContainsOutputFolder()
    {
        Make().Install(Sample());

        var config = File.ReadAllText(Path.Combine(_root, "my-skill-config"));
        Assert.Contains(@"C:\output\my-skill", config);
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
}
