namespace PdfExtractToSkill.Application.Interfaces;

public interface ISkillInstaller
{
    void Install(SkillDefinition definition);
    bool Exists(string skillName);
}
