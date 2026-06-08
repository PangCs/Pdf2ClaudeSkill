using System.Diagnostics;
using PdfExtractToSkill.Application;
using PdfExtractToSkill.Infrastructure.Python;

namespace PdfExtractToSkill.Infrastructure.Tests.Python;

public class PythonRunnerTests
{
    private static (PythonRunner runner, List<ProcessStartInfo> captured) CreateRunner(ProcessResult result)
    {
        var captured = new List<ProcessStartInfo>();
        var runner = new PythonRunner(info => { captured.Add(info); return result; });
        return (runner, captured);
    }

    [Fact]
    public void Run_SetsExecutablePath()
    {
        var (runner, captured) = CreateRunner(new ProcessResult(0, "", ""));

        runner.Run(@"C:\Python\python.exe", "script.py", []);

        Assert.Equal(@"C:\Python\python.exe", captured[0].FileName);
    }

    [Fact]
    public void Run_PlacesScriptPathFirstInArgumentList()
    {
        var (runner, captured) = CreateRunner(new ProcessResult(0, "", ""));

        runner.Run(@"C:\Python\python.exe", "script.py", ["arg1", "arg2"]);

        Assert.Equal(["script.py", "arg1", "arg2"], captured[0].ArgumentList);
    }

    [Fact]
    public void Run_HandlesEmptyArgs()
    {
        var (runner, captured) = CreateRunner(new ProcessResult(0, "", ""));

        runner.Run(@"C:\Python\python.exe", "script.py", []);

        Assert.Equal(["script.py"], captured[0].ArgumentList);
    }

    [Fact]
    public void Run_HandlesArgsAndScriptPathsWithSpaces()
    {
        var (runner, captured) = CreateRunner(new ProcessResult(0, "", ""));

        runner.Run(@"C:\Python\python.exe", @"C:\my scripts\extract.py", [@"C:\some path\file.pdf"]);

        Assert.Equal([@"C:\my scripts\extract.py", @"C:\some path\file.pdf"], captured[0].ArgumentList);
    }

    [Fact]
    public void Run_RedirectsOutputAndErrorWithoutShellExecute()
    {
        var (runner, captured) = CreateRunner(new ProcessResult(0, "", ""));

        runner.Run(@"C:\Python\python.exe", "script.py", []);

        Assert.True(captured[0].RedirectStandardOutput);
        Assert.True(captured[0].RedirectStandardError);
        Assert.False(captured[0].UseShellExecute);
    }

    [Fact]
    public void Run_ReturnsExitCodeFromProcess()
    {
        var (runner, _) = CreateRunner(new ProcessResult(42, "", ""));

        var result = runner.Run(@"C:\Python\python.exe", "script.py", []);

        Assert.Equal(42, result.ExitCode);
    }

    [Fact]
    public void Run_ReturnsCapturedStdoutAndStderr()
    {
        var (runner, _) = CreateRunner(new ProcessResult(0, "hello stdout", "hello stderr"));

        var result = runner.Run(@"C:\Python\python.exe", "script.py", []);

        Assert.Equal("hello stdout", result.Stdout);
        Assert.Equal("hello stderr", result.Stderr);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Run_Integration_ExecutesPythonAndCapturesOutput()
    {
        var detector = new PythonDetector();
        var pythonPath = detector.TryDetect();
        Assert.NotNull(pythonPath);

        var runner = new PythonRunner();
        var result = runner.Run(pythonPath, "-c", ["print('hello')"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello", result.Stdout);
    }
}
