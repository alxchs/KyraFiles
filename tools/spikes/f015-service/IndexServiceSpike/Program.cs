using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;

// Spike descartável — F015 T1. Não entra em src/.
// Responde "ping" num named pipe FilesCustom.IndexService.Spike.
// Roda como SYSTEM; pipe acessível a qualquer usuário autenticado.

if (args.Length > 0 && args[0] == "--service")
{
    ServiceBase.Run(new IndexServiceSpike());
}
else
{
    // Modo console: útil para testar o servidor sem instalar o serviço.
    Console.WriteLine("IndexServiceSpike iniciando em modo console (Ctrl+C para sair)...");
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
    await PipeServer.RunAsync(cts.Token);
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Loop do servidor de named pipe — reutilizado pelo modo console e pelo serviço.
/// </summary>
internal static class PipeServer
{
    internal const string PipeName = "FilesCustom.IndexService.Spike";

    internal static async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // ACL: permitir todos os usuários autenticados locais (leitura/escrita).
            var ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));

            await using var pipe = NamedPipeServerStreamAcl.Create(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous,
                0, 0,
                ps);

            try
            {
                await pipe.WaitForConnectionAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // Tratar a conexão num Task separado para aceitar outra logo.
            _ = Task.Run(async () =>
            {
                try
                {
                    using var sr = new StreamReader(pipe);
                    await using var sw = new StreamWriter(pipe) { AutoFlush = true };

                    string? line = await sr.ReadLineAsync();
                    if (line?.Trim().Equals("ping", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        string identity = WindowsIdentity.GetCurrent().Name;
                        await sw.WriteLineAsync($"pong:{identity}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[pipe handler] {ex.Message}");
                }
            }, token);
        }
    }
}

/// <summary>
/// Wrapper ServiceBase — usa o loop de pipe acima como implementação.
/// </summary>
internal sealed class IndexServiceSpike : ServiceBase
{
    private CancellationTokenSource? _cts;
    private Task? _pipeTask;

    public IndexServiceSpike()
    {
        ServiceName = "FilesCustomIndexServiceSpike";
        CanStop = true;
    }

    protected override void OnStart(string[] args)
    {
        _cts = new CancellationTokenSource();
        _pipeTask = PipeServer.RunAsync(_cts.Token);
    }

    protected override void OnStop()
    {
        _cts?.Cancel();
        try { _pipeTask?.Wait(TimeSpan.FromSeconds(5)); } catch { }
    }
}
