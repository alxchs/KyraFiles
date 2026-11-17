<#
.SYNOPSIS
    F015 T2a — Sonda da MFT bruta (requer admin / SYSTEM).
    Abre \\.\C:, lê dados do volume NTFS e extents da $MFT, conta registros em uso.

.DESCRIPTION
    PRECISA DE ADMINISTRADOR. Rodado pelo Claude com:
        Start-Process pwsh -Verb RunAs -ArgumentList "-NoProfile -File f015_mft_probe.ps1"
    O Alexandre só clica "Sim" no UAC.
    Resultado gravado em log para o Claude ler.

.NOTES
    Não altera nenhum arquivo. Só leitura.
    Grava saída em %TEMP%\claude\desenv\utils\KyraFiles\f015\
#>

$LogDir  = "$env:TEMP\claude\desenv\utils\KyraFiles\f015"
$LogFile = Join-Path $LogDir "f015_mft_probe-$(Get-Date -f 'yyyyMMdd_HHmmss').log"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Log([string]$msg) {
    $line = "[$(Get-Date -f 'HH:mm:ss.fff')] $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

Log "=== f015_mft_probe.ps1 (admin) ==="
Log "Log: $LogFile"
Log "Identidade: $([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)"

#
# Implementação via C# inline (Add-Type) para acessar as APIs nativas.
# FSCTL_GET_NTFS_VOLUME_DATA  = 0x00090064
# FSCTL_GET_RETRIEVAL_POINTERS = 0x00090073
#

$src = @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

public static class MftProbe
{
    private const uint GENERIC_READ          = 0x80000000;
    private const uint FILE_SHARE_ALL        = 7;
    private const int  OPEN_EXISTING         = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

    // FSCTL codes
    private const uint FSCTL_GET_NTFS_VOLUME_DATA   = 0x00090064;
    private const uint FSCTL_GET_RETRIEVAL_POINTERS  = 0x00090073;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string n, uint a, uint s, IntPtr sec, int c, uint f, IntPtr t);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(IntPtr h, uint code,
        IntPtr inBuf, uint inSz, IntPtr outBuf, uint outSz, out uint bytes, IntPtr ov);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr h);

    [StructLayout(LayoutKind.Sequential)]
    public struct NTFS_VOLUME_DATA_BUFFER
    {
        public long  VolumeSerialNumber;
        public long  NumberSectors;
        public long  TotalClusters;
        public long  FreeClusters;
        public long  TotalReserved;
        public uint  BytesPerSector;
        public uint  BytesPerCluster;
        public uint  BytesPerFileRecordSegment;
        public uint  ClustersPerFileRecordSegment;
        public long  MftValidDataLength;
        public long  MftStartLcn;
        public long  Mft2StartLcn;
        public long  MftZoneStart;
        public long  MftZoneEnd;
        // extended fields omitted
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTING_VCN_INPUT_BUFFER { public long StartingVcn; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RETRIEVAL_POINTERS_BUFFER
    {
        public uint ExtentCount;
        public uint _pad;
        public long StartingVcn;
        // extents follow: (NextVcn, Lcn) pairs
    }

    public static string Run(string volume)
    {
        var sb = new System.Text.StringBuilder();
        // volume ex: @"\\.\C:"
        IntPtr hVol = CreateFile(volume, GENERIC_READ, FILE_SHARE_ALL, IntPtr.Zero, OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        if (hVol == new IntPtr(-1))
            return $"ERRO: CreateFile({volume}) falhou — HR={Marshal.GetLastWin32Error()}";

        try
        {
            // --- NTFS_VOLUME_DATA ---
            int vdSz = Marshal.SizeOf<NTFS_VOLUME_DATA_BUFFER>() + 128; // margem
            IntPtr vdBuf = Marshal.AllocHGlobal(vdSz);
            try
            {
                uint retBytes;
                bool ok = DeviceIoControl(hVol, FSCTL_GET_NTFS_VOLUME_DATA,
                    IntPtr.Zero, 0, vdBuf, (uint)vdSz, out retBytes, IntPtr.Zero);
                if (!ok)
                    return $"ERRO FSCTL_GET_NTFS_VOLUME_DATA: HR={Marshal.GetLastWin32Error()}";

                var vd = Marshal.PtrToStructure<NTFS_VOLUME_DATA_BUFFER>(vdBuf);
                sb.AppendLine($"BytesPerSector:            {vd.BytesPerSector}");
                sb.AppendLine($"BytesPerCluster:           {vd.BytesPerCluster}");
                sb.AppendLine($"BytesPerFileRecordSegment: {vd.BytesPerFileRecordSegment}");
                sb.AppendLine($"MftValidDataLength:        {vd.MftValidDataLength / (1024*1024):N0} MB  ({vd.MftValidDataLength:N0} bytes)");
                sb.AppendLine($"MftStartLcn:               {vd.MftStartLcn}");
                long mftRecords = vd.BytesPerFileRecordSegment > 0
                    ? vd.MftValidDataLength / vd.BytesPerFileRecordSegment
                    : -1;
                sb.AppendLine($"MFT registros (estimado):  {mftRecords:N0}  (MftValidDataLength / BytesPerFileRecordSegment)");

                // --- Ler MFT em blocos e contar registros em uso ---
                // Abre \\.\C:\$MFT para obter os extents e medir a velocidade de leitura
                string mftPath = volume.TrimEnd(':') + @":\$MFT";
                if (volume.StartsWith(@"\\.\")) mftPath = volume + @"\$MFT";

                IntPtr hMft = CreateFile(mftPath, GENERIC_READ, FILE_SHARE_ALL, IntPtr.Zero, OPEN_EXISTING,
                    FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
                if (hMft == new IntPtr(-1))
                {
                    sb.AppendLine($"[AVISO] Não foi possível abrir {mftPath}: HR={Marshal.GetLastWin32Error()} — velocidade de leitura não medida");
                    return sb.ToString();
                }
                try
                {
                    // Extents da MFT via FSCTL_GET_RETRIEVAL_POINTERS
                    int rpBufSz = 4096;
                    IntPtr rpBuf = Marshal.AllocHGlobal(rpBufSz);
                    IntPtr inBuf = Marshal.AllocHGlobal(8);
                    Marshal.WriteInt64(inBuf, 0); // StartingVcn = 0

                    long totalExtentVcns = 0;
                    int extentCount = 0;
                    try
                    {
                        while (true)
                        {
                            uint rb;
                            bool rok = DeviceIoControl(hMft, FSCTL_GET_RETRIEVAL_POINTERS,
                                inBuf, 8, rpBuf, (uint)rpBufSz, out rb, IntPtr.Zero);
                            int hr = Marshal.GetLastWin32Error();
                            if (!rok && hr != 234 /*ERROR_MORE_DATA*/) break;

                            uint extCnt = (uint)Marshal.ReadInt32(rpBuf, 0);
                            long startVcn = Marshal.ReadInt64(rpBuf, 8);
                            extentCount += (int)extCnt;

                            long nextVcn = startVcn;
                            for (int i = 0; i < (int)extCnt; i++)
                            {
                                long nv  = Marshal.ReadInt64(rpBuf, 16 + i * 16);
                                totalExtentVcns += nv - nextVcn;
                                nextVcn = nv;
                                Marshal.WriteInt64(inBuf, nv); // nova StartingVcn
                            }
                            if (!rok) break; // ERROR_MORE_DATA esgotado
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(rpBuf);
                        Marshal.FreeHGlobal(inBuf);
                    }

                    sb.AppendLine($"MFT extents:               {extentCount}  (fragmentação: {(extentCount > 1 ? "sim" : "não")})");
                    long mftSizeVCN = totalExtentVcns * vd.BytesPerCluster;
                    sb.AppendLine($"MFT tamanho (extents):     {mftSizeVCN / (1024*1024):N0} MB");

                    // Leitura sequencial da MFT em blocos de 2 MB para medir MB/s
                    long recSize   = vd.BytesPerFileRecordSegment > 0 ? vd.BytesPerFileRecordSegment : 1024;
                    int  blockSize = 2 * 1024 * 1024; // 2 MB
                    byte[] block   = new byte[blockSize];
                    long totalBytesRead = 0;
                    long recordsInUse   = 0;

                    using var mftStream = new FileStream(
                        new Microsoft.Win32.SafeHandles.SafeFileHandle(hMft, false),
                        FileAccess.Read, blockSize, false);

                    mftStream.Seek(0, SeekOrigin.Begin);

                    var sw = Stopwatch.StartNew();
                    int bytesRead;
                    while ((bytesRead = mftStream.Read(block, 0, blockSize)) > 0)
                    {
                        totalBytesRead += bytesRead;
                        // Verificar quais registros estão em uso pela assinatura "FILE" e flag In-Use (bit 0 de flags @ offset 22)
                        for (long off = 0; off + recSize <= bytesRead; off += recSize)
                        {
                            if (block[off] == 'F' && block[off+1] == 'I' && block[off+2] == 'L' && block[off+3] == 'E')
                            {
                                ushort flags = (ushort)(block[off + 22] | (block[off + 23] << 8));
                                if ((flags & 1) != 0) recordsInUse++;
                            }
                        }
                    }
                    sw.Stop();

                    double mbRead = totalBytesRead / (1024.0 * 1024.0);
                    double mbps   = sw.Elapsed.TotalSeconds > 0 ? mbRead / sw.Elapsed.TotalSeconds : 0;
                    sb.AppendLine($"MFT lida:                  {mbRead:N1} MB em {sw.Elapsed.TotalSeconds:F2} s  ({mbps:N0} MB/s)");
                    sb.AppendLine($"Registros em uso:          {recordsInUse:N0}  (assinatura FILE + flag In-Use)");
                }
                finally
                {
                    // handle managed por SafeFileHandle acima — não fechar duas vezes
                }
            }
            finally
            {
                Marshal.FreeHGlobal(vdBuf);
            }
        }
        finally
        {
            CloseHandle(hVol);
        }

        return sb.ToString();
    }
}
'@

try {
    Add-Type -TypeDefinition $src -Language CSharp -ErrorAction Stop
} catch {
    Log "ERRO ao compilar MftProbe: $_"
    exit 1
}

Log ""
Log "--- Sondando \\.\C: ---"
$sw = [Diagnostics.Stopwatch]::StartNew()
$result = [MftProbe]::Run('\\.\C:')
$sw.Stop()
Log $result
Log "Tempo total: $($sw.Elapsed.TotalSeconds.ToString('F2')) s"
Log ""
Log "Comando: [MftProbe]::Run('\\.\C:')"
Log "Log completo: $LogFile"
Log "=== Concluído ==="
Write-Host "Log: $LogFile"
