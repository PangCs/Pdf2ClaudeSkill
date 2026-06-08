using PdfExtractToSkill.Application.Interfaces;
using PdfExtractToSkill.Infrastructure.Skill;
using PdfExtractToSkill.ViewModels;

namespace PdfExtractToSkill.Infrastructure.Tests.ViewModels;

public class ConfirmationDialogViewModelTests : IDisposable
{
    private readonly string _skillsRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_skillsRoot))
            Directory.Delete(_skillsRoot, recursive: true);
    }

    private ConfirmationDialogViewModel Make(string pdfPath = @"C:\docs\Rorze_EFEM.pdf")
    {
        var installer = new SkillInstaller(_skillsRoot);
        var deriver = new PdfExtractToSkill.Infrastructure.Skill.SkillNameDeriver();
        return new ConfirmationDialogViewModel(installer, deriver, pdfPath);
    }

    [Fact]
    public void Constructor_PrefillsSkillNameFromPdfFileName()
    {
        var vm = Make(@"C:\docs\Rorze_EFEM.pdf");

        Assert.Equal("rorze-efem", vm.SkillName);
    }

    [Fact]
    public void Constructor_PrefillsDescription()
    {
        var vm = Make();

        Assert.NotEmpty(vm.Description);
    }

    [Fact]
    public void ConfirmCommand_DisabledWhenSkillNameEmpty()
    {
        var vm = Make();
        vm.SkillName = string.Empty;

        Assert.False(vm.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmCommand_DisabledWhenSkillNameInvalidKebabCase()
    {
        var vm = Make();
        vm.SkillName = "INVALID NAME!";

        Assert.False(vm.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmCommand_EnabledWhenSkillNameValidKebabCase()
    {
        var vm = Make();
        vm.SkillName = "valid-name";

        Assert.True(vm.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void SkillAlreadyExists_TrueWhenSkillInstalled()
    {
        var installer = new SkillInstaller(_skillsRoot);
        installer.Install(new PdfExtractToSkill.Application.SkillDefinition(
            "existing-skill", "desc", @"C:\out\x.md", "x.pdf"));

        var deriver = new SkillNameDeriver();
        var vm = new ConfirmationDialogViewModel(installer, deriver, @"C:\docs\existing-skill.pdf");

        Assert.True(vm.SkillAlreadyExists);
    }

    [Fact]
    public void ConfirmCommand_DisabledWhenSkillExistsAndOverwriteNotConfirmed()
    {
        var installer = new SkillInstaller(_skillsRoot);
        installer.Install(new PdfExtractToSkill.Application.SkillDefinition(
            "existing-skill", "desc", @"C:\out\x.md", "x.pdf"));

        var deriver = new SkillNameDeriver();
        var vm = new ConfirmationDialogViewModel(installer, deriver, @"C:\docs\existing-skill.pdf");
        vm.OverwriteConfirmed = false;

        Assert.False(vm.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmCommand_EnabledWhenSkillExistsAndOverwriteConfirmed()
    {
        var installer = new SkillInstaller(_skillsRoot);
        installer.Install(new PdfExtractToSkill.Application.SkillDefinition(
            "existing-skill", "desc", @"C:\out\x.md", "x.pdf"));

        var deriver = new SkillNameDeriver();
        var vm = new ConfirmationDialogViewModel(installer, deriver, @"C:\docs\existing-skill.pdf");
        vm.OverwriteConfirmed = true;

        Assert.True(vm.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmCommand_FiresCloseRequestedWithResult()
    {
        var vm = Make();
        vm.SkillName = "my-skill";
        vm.Description = "my description";
        ConfirmationResult? result = null;
        vm.CloseRequested += (_, r) => result = r;

        vm.ConfirmCommand.Execute(null);

        Assert.NotNull(result);
        Assert.Equal("my-skill", result!.SkillName);
        Assert.Equal("my description", result.Description);
    }

    [Fact]
    public void CancelCommand_FiresCloseRequestedWithNull()
    {
        var vm = Make();
        ConfirmationResult? result = new ConfirmationResult("x", "y", false);
        vm.CloseRequested += (_, r) => result = r;

        vm.CancelCommand.Execute(null);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("valid-name", true)]
    [InlineData("abc", true)]
    [InlineData("a1b2", true)]
    [InlineData("a-b-c", true)]
    [InlineData("", false)]
    [InlineData("UPPER", false)]
    [InlineData("has space", false)]
    [InlineData("-starts-with-hyphen", false)]
    [InlineData("ends-with-hyphen-", false)]
    [InlineData("double--hyphen", false)]
    public void IsValidKebabCase_ReturnsExpected(string value, bool expected)
    {
        Assert.Equal(expected, ConfirmationDialogViewModel.IsValidKebabCase(value));
    }
}
