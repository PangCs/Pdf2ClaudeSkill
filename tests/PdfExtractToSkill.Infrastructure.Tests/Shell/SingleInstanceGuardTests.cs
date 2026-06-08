using PdfExtractToSkill.Infrastructure.Shell;

namespace PdfExtractToSkill.Infrastructure.Tests.Shell;

public class SingleInstanceGuardTests
{
    private static (string mutex, string pipe) UniquePair()
    {
        var id = Guid.NewGuid().ToString("N");
        return ($"Local\\Test_{id}", $"TestPipe_{id}");
    }

    [Fact]
    public void FirstInstance_IsOwner()
    {
        var (mutex, pipe) = UniquePair();
        using var guard = new SingleInstanceGuard(mutex, pipe);

        Assert.True(guard.IsOwner);
    }

    [Fact]
    public void SecondInstance_IsNotOwner()
    {
        var (mutex, pipe) = UniquePair();
        using var first = new SingleInstanceGuard(mutex, pipe);
        using var second = new SingleInstanceGuard(mutex, pipe);

        Assert.False(second.IsOwner);
    }

    [Fact]
    public async Task ForwardActivation_FirstInstanceReceivesMessage()
    {
        var (mutex, pipe) = UniquePair();
        using var first = new SingleInstanceGuard(mutex, pipe);
        // give the pipe server task a moment to start listening
        await Task.Delay(100);

        var received = new TaskCompletionSource<string>();
        first.Activated += (_, msg) => received.TrySetResult(msg);

        using var second = new SingleInstanceGuard(mutex, pipe);
        second.ForwardActivation("pdfextracttoskill://action?file=test.pdf");

        var result = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("pdfextracttoskill://action?file=test.pdf", result);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var (mutex, pipe) = UniquePair();
        var guard = new SingleInstanceGuard(mutex, pipe);

        var exception = Record.Exception(() => guard.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void AfterDispose_NewInstanceBecomesOwner()
    {
        var (mutex, pipe) = UniquePair();
        var first = new SingleInstanceGuard(mutex, pipe);
        first.Dispose();

        using var second = new SingleInstanceGuard(mutex, pipe);
        Assert.True(second.IsOwner);
    }
}
