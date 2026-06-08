using System.Diagnostics;
using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Python;

public sealed class PythonRunner : IPythonRunner
{
    private readonly Func<ProcessStartInfo, ProcessResult> _execute;

    public PythonRunner() : this(Execute) { }

    internal PythonRunner(Func<ProcessStartInfo, ProcessResult> execute)
    {
        _execute = execute;
    }

    public ProcessResult Run(string executablePath, string scriptPath, string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(scriptPath);
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        return _execute(startInfo);
    }

    private static ProcessResult Execute(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {startInfo.FileName}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, stdoutTask.Result, stderrTask.Result);
    }
}
