namespace PdfExtractToSkill.Application;

public sealed record ExtractionRequest(
    string PdfPath,
    string OutputPath,
    string SkillName,
    string Description,
    string PythonExePath,
    bool Overwrite
);
