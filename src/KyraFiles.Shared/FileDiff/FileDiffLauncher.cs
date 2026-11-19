using System;
using System.Diagnostics;
using System.IO;

namespace KyraFiles.Shared.FileDiff
{
    public static class FileDiffLauncher
    {
        public static (bool Success, string Message) LaunchDiff(DiffToolDefinition tool, string leftPath, string rightPath)
        {
            if (tool == null)
            {
                return (false, "Nenhuma ferramenta de comparacao selecionada.");
            }

            if (string.IsNullOrWhiteSpace(tool.Command))
            {
                return (false, "Comando da ferramenta de comparacao esta vazio.");
            }

            if (!File.Exists(leftPath) && !Directory.Exists(leftPath))
            {
                return (false, $"O caminho a esquerda nao existe: {leftPath}");
            }

            if (!File.Exists(rightPath) && !Directory.Exists(rightPath))
            {
                return (false, $"O caminho a direita nao existe: {rightPath}");
            }

            try
            {
                var psi = BuildStartInfo(tool, leftPath, rightPath);
                var process = Process.Start(psi);
                if (process == null)
                {
                    return (false, "Falha ao iniciar o processo da ferramenta de comparacao.");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao abrir ferramenta de comparacao: {ex.Message}");
            }
        }

        public static ProcessStartInfo BuildStartInfo(DiffToolDefinition tool, string leftPath, string rightPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = tool.Command,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            string template = string.IsNullOrWhiteSpace(tool.ArgumentsTemplate)
                ? "\"{left}\" \"{right}\""
                : tool.ArgumentsTemplate;

            // Substitui marcadores {left}/{esquerda} e {right}/{direita}
            string formattedArgs = template
                .Replace("{left}", leftPath, StringComparison.OrdinalIgnoreCase)
                .Replace("{esquerda}", leftPath, StringComparison.OrdinalIgnoreCase)
                .Replace("{right}", rightPath, StringComparison.OrdinalIgnoreCase)
                .Replace("{direita}", rightPath, StringComparison.OrdinalIgnoreCase);

            psi.Arguments = formattedArgs;
            return psi;
        }
    }
}
