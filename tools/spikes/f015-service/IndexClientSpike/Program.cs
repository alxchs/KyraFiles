using System.IO.Pipes;

// Spike descartável — F015 T1. Cliente não elevado.
// Conecta ao named pipe do IndexServiceSpike e faz "ping".

const string PipeName = "FilesCustom.IndexService.Spike";
const int TimeoutMs   = 5_000;

Console.WriteLine($"Conectando ao pipe '{PipeName}'...");

try
{
    await using var pipe = new NamedPipeClientStream(
        ".",
        PipeName,
        PipeDirection.InOut,
        PipeOptions.Asynchronous);

    await pipe.ConnectAsync(TimeoutMs);
    pipe.ReadMode = PipeTransmissionMode.Message;

    using var sr = new StreamReader(pipe);
    await using var sw = new StreamWriter(pipe) { AutoFlush = true };

    await sw.WriteLineAsync("ping");

    string? response = await sr.ReadLineAsync();
    Console.WriteLine($"Resposta do servidor: {response}");

    if (response?.StartsWith("pong:", StringComparison.OrdinalIgnoreCase) == true)
    {
        Console.WriteLine("[OK] Ping/Pong bem-sucedido.");
        return 0;
    }
    else
    {
        Console.Error.WriteLine("[ERRO] Resposta inesperada.");
        return 1;
    }
}
catch (TimeoutException)
{
    Console.Error.WriteLine($"[ERRO] Timeout ao conectar ao pipe '{PipeName}'.");
    Console.Error.WriteLine("O serviço está rodando? Use 'sc query FilesCustomIndexServiceSpike'.");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERRO] {ex.GetType().Name}: {ex.Message}");
    return 3;
}
