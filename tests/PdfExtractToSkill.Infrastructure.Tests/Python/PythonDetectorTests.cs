using PdfExtractToSkill.Infrastructure.Python;

namespace PdfExtractToSkill.Infrastructure.Tests.Python;

public class PythonDetectorTests
{
    private static string Exe(string dir, string name) => Path.Combine(dir, name + ".exe");

    [Fact]
    public void TryDetect_WhenPyExePresent_ReturnsPyPath()
    {
        const string dir = @"C:\Python";
        var detector = new PythonDetector([dir], path => path == Exe(dir, "py"));

        Assert.Equal(Exe(dir, "py"), detector.TryDetect());
    }

    [Fact]
    public void TryDetect_WhenOnlyPythonExePresent_ReturnsPythonPath()
    {
        const string dir = @"C:\Python";
        var detector = new PythonDetector([dir], path => path == Exe(dir, "python"));

        Assert.Equal(Exe(dir, "python"), detector.TryDetect());
    }

    [Fact]
    public void TryDetect_PrefersPyOverPython()
    {
        const string dir = @"C:\Python";
        var detector = new PythonDetector(
            [dir],
            path => path == Exe(dir, "py") || path == Exe(dir, "python"));

        Assert.Equal(Exe(dir, "py"), detector.TryDetect());
    }

    [Fact]
    public void TryDetect_PrefersPythonOverPython3()
    {
        const string dir = @"C:\Python";
        var detector = new PythonDetector(
            [dir],
            path => path == Exe(dir, "python") || path == Exe(dir, "python3"));

        Assert.Equal(Exe(dir, "python"), detector.TryDetect());
    }

    [Fact]
    public void TryDetect_FindsPython3WhenOthersAbsent()
    {
        const string dir = @"C:\Python";
        var detector = new PythonDetector([dir], path => path == Exe(dir, "python3"));

        Assert.Equal(Exe(dir, "python3"), detector.TryDetect());
    }

    [Fact]
    public void TryDetect_ReturnsFirstMatchingDirInPath()
    {
        const string dir1 = @"C:\Python\old";
        const string dir2 = @"C:\Python\new";
        var detector = new PythonDetector(
            [dir1, dir2],
            path => path == Exe(dir1, "py") || path == Exe(dir2, "py"));

        var result = detector.TryDetect();
        Assert.NotNull(result);
        Assert.Equal(Exe(dir1, "py"), result);
    }

    [Fact]
    public void TryDetect_WhenNothingFound_ReturnsNull()
    {
        var detector = new PythonDetector([@"C:\NoSuchDir"], _ => false);

        Assert.Null(detector.TryDetect());
    }

    [Fact]
    public void TryDetect_WhenPathEmpty_ReturnsNull()
    {
        var detector = new PythonDetector([], _ => false);

        Assert.Null(detector.TryDetect());
    }

    [Fact]
    public void TryDetect_TrimsWhitespaceFromPathEntries()
    {
        const string dir = @"C:\Python";
        var detector = new PythonDetector([$"  {dir}  "], path => path == Exe(dir, "py"));

        Assert.Equal(Exe(dir, "py"), detector.TryDetect());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void TryDetect_Integration_FindsPythonInCurrentEnvironment()
    {
        var result = new PythonDetector().TryDetect();

        Assert.NotNull(result);
        Assert.True(File.Exists(result), $"Detected path does not exist: {result}");
    }
}
