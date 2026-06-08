namespace PdfExtractToSkill.Application;

public sealed record ExtractionResult(
    bool Success,
    string SkillName,
    string? OutputFilePath,
    string? ErrorMessage
);
