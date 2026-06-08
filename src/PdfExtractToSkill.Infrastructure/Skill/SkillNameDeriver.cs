using System.Text.RegularExpressions;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Skill;

public sealed class SkillNameDeriver : ISkillNameDeriver
{
    private static readonly Regex NonAlphanumeric = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public string DeriveFrom(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var lower = stem.ToLowerInvariant();
        // replace non-alphanumeric runs (spaces, underscores, dots, etc.) with hyphens
        var hyphenated = NonAlphanumeric.Replace(lower, "-");
        return hyphenated.Trim('-');
    }
}
