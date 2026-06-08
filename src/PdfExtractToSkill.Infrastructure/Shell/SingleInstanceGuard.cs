using System.IO.Pipes;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Shell;

public sealed class SingleInstanceGuard : ISingleInstanceGuard
{
    private const string DefaultMutexName = "Global\\PdfExtractToSkillInstance";
    private const string DefaultPipeName  = "PdfExtractToSkillActivation";

    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private CancellationTokenSource? _cts;

    public bool IsOwner { get; }
    public event EventHandler<string>? Activated;

    public SingleInstanceGuard()
        : this(DefaultMutexName, DefaultPipeName) { }

    internal SingleInstanceGuard(string mutexName, string pipeName)
    {
        _pipeName = pipeName;
        _mutex = new Mutex(initiallyOwned: true, name: mutexName, out var createdNew);
        IsOwner = createdNew;
        if (IsOwner)
            StartPipeServer();
    }

    public void ForwardActivation(string args)
    {
        using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
        client.Connect(timeout: 2000);
        using var writer = new StreamWriter(client) { AutoFlush = true };
        writer.WriteLine(args);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _mutex.Dispose();
    }

    private void StartPipeServer()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => RunPipeServerAsync(_cts.Token));
    }

    private async Task RunPipeServerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                using var reader = new StreamReader(server);
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is not null)
                    Activated?.Invoke(this, line);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                // pipe broken; loop and listen again
            }
        }
    }
}
