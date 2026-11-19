using System;
using System.IO;

namespace KyraFiles.Shared.FileDiff
{
    public static class BinaryDetector
    {
        private const int DefaultBufferScanSize = 4096;

        public static bool IsBinaryFile(string path, int maxBytesToScan = DefaultBufferScanSize)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                int bytesToRead = (int)Math.Min(fs.Length, maxBytesToScan);
                if (bytesToRead == 0)
                {
                    return false;
                }

                byte[] buffer = new byte[bytesToRead];
                int read = fs.Read(buffer, 0, bytesToRead);

                for (int i = 0; i < read; i++)
                {
                    // Byte 0x00 e indicador comum de arquivo binario (executavel, imagem, zip, dll)
                    if (buffer[i] == 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
