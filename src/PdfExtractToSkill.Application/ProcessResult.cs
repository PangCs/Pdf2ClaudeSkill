namespace PdfExtractToSkill.Application;

public sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);
