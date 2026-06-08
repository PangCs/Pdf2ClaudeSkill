using PdfExtractToSkill.Infrastructure.Skill;

namespace PdfExtractToSkill.Infrastructure.Tests.Skill;

public class SkillNameDeriverTests
{
    private static readonly SkillNameDeriver Deriver = new();

    [Theory]
    [InlineData("Rorze_EFEM_CommSpec_basic.pdf",   "rorze-efem-commspec-basic")]
    [InlineData("My Document.pdf",                  "my-document")]
    [InlineData("hello-world.pdf",                  "hello-world")]
    [InlineData("UPPER_CASE_FILE.pdf",              "upper-case-file")]
    [InlineData("multiple   spaces.pdf",            "multiple-spaces")]
    [InlineData("dots.in.name.pdf",                 "dots-in-name")]
    [InlineData("mixed_Under-and-Hyph.pdf",         "mixed-under-and-hyph")]
    [InlineData("123-numeric-start.pdf",            "123-numeric-start")]
    [InlineData("spec(v2.1).pdf",                   "spec-v2-1")]
    [InlineData("__leading_underscores__.pdf",      "leading-underscores")]
    [InlineData("  spaces around  .pdf",            "spaces-around")]
    public void DeriveFrom_ProducesExpectedKebabCase(string fileName, string expected)
    {
        Assert.Equal(expected, Deriver.DeriveFrom(fileName));
    }

    [Fact]
    public void DeriveFrom_StripsPdfExtension()
    {
        Assert.Equal("my-file", Deriver.DeriveFrom("My File.pdf"));
    }

    [Fact]
    public void DeriveFrom_AlreadyKebabCase_IsUnchanged()
    {
        Assert.Equal("already-kebab", Deriver.DeriveFrom("already-kebab.pdf"));
    }

    [Fact]
    public void DeriveFrom_CollapseRepeatedHyphens()
    {
        Assert.Equal("a-b-c", Deriver.DeriveFrom("a___b___c.pdf"));
    }

    [Fact]
    public void DeriveFrom_TrimsLeadingAndTrailingHyphens()
    {
        Assert.Equal("trimmed", Deriver.DeriveFrom("-trimmed-.pdf"));
    }
}
