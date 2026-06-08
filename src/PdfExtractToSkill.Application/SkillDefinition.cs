namespace PdfExtractToSkill.Application;

public sealed record SkillDefinition(
    string Name,
    string Description,
    string OutputFilePath,
    string SourceFileName
);
