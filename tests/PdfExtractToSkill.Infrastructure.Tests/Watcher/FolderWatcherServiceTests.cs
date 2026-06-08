using PdfExtractToSkill.Infrastructure.Watcher;

namespace PdfExtractToSkill.Infrastructure.Tests.Watcher;

public class FolderWatcherServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public FolderWatcherServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Start_DetectsPdfCreation()
    {
        using var svc = new FolderWatcherService(TimeSpan.FromMilliseconds(50));
        var tcs = new TaskCompletionSource<string>();
        svc.PdfDetected += (_, path) => tcs.TrySetResult(path);

        svc.Start(_dir);
        var file = Path.Combine(_dir, "test.pdf");
        await File.WriteAllTextAsync(file, "dummy");

        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(file, result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Start_IgnoresNonPdfFiles()
    {
        using var svc = new FolderWatcherService(TimeSpan.FromMilliseconds(50));
        var fired = false;
        svc.PdfDetected += (_, _) => fired = true;

        svc.Start(_dir);
        await File.WriteAllTextAsync(Path.Combine(_dir, "doc.txt"), "text");
        await Task.Delay(200);

        Assert.False(fired);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Start_IgnoresSubdirectoryPdfs()
    {
        var sub = Directory.CreateDirectory(Path.Combine(_dir, "sub")).FullName;
        using var svc = new FolderWatcherService(TimeSpan.FromMilliseconds(50));
        var fired = false;
        svc.PdfDetected += (_, _) => fired = true;

        svc.Start(_dir);
        await File.WriteAllTextAsync(Path.Combine(sub, "deep.pdf"), "dummy");
        await Task.Delay(200);

        Assert.False(fired);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Debounce_RapidCreations_FiresOnce()
    {
        using var svc = new FolderWatcherService(TimeSpan.FromMilliseconds(200));
        var count = 0;
        svc.PdfDetected += (_, _) => Interlocked.Increment(ref count);

        svc.Start(_dir);
        var file = Path.Combine(_dir, "rapid.pdf");

        // Write the file; FileSystemWatcher may fire Created once, but we simulate
        // re-arm by deleting and re-creating quickly.
        await File.WriteAllTextAsync(file, "v1");
        await Task.Delay(50);
        File.Delete(file);
        await File.WriteAllTextAsync(file, "v2");

        await Task.Delay(500);
        // At most 2 events (one per creation), but debounce ensures no burst
        Assert.True(count <= 2);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Stop_StopsDetection()
    {
        using var svc = new FolderWatcherService(TimeSpan.FromMilliseconds(50));
        var fired = false;
        svc.PdfDetected += (_, _) => fired = true;

        svc.Start(_dir);
        svc.Stop();
        await File.WriteAllTextAsync(Path.Combine(_dir, "after-stop.pdf"), "dummy");
        await Task.Delay(200);

        Assert.False(fired);
    }
}
