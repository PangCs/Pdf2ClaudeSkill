namespace PdfExtractToSkill.Application.Interfaces;

public interface IPythonRunner
{
    ProcessResult Run(string executablePath, string scriptPath, string[] args);
}
