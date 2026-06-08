namespace PdfExtractToSkill.Application.Interfaces;

public interface ISkillNameDeriver
{
    string DeriveFrom(string fileName);
}
