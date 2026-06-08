namespace PdfExtractToSkill.Application;

public sealed record PrerequisiteReport(
    bool PythonFound,
    string? PythonPath,
    bool PyMuPdfInstalled,
    string? PyMuPdfInstallError
);
