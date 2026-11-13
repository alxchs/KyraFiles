# tools/perf/f092_test_shcreate.ps1
# Script de diagnóstico para F092 T0: teste de SHCreateItemFromParsingName para itens de This PC
$code = @"
using System;
using System.Runtime.InteropServices;

public class ShellTester
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [Out, MarshalAs(UnmanagedType.Interface)] out object ppv);

    public static string TestPath(string path)
    {
        Guid IShellItemGuid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
        int hr = SHCreateItemFromParsingName(path, IntPtr.Zero, IShellItemGuid, out object item);
        if (hr == 0 && item != null)
        {
            return "SUCCESS: hr=0, item=" + item.GetType().FullName;
        }
        return "FAILED: hr=0x" + hr.ToString("X8");
    }
}
"@

Add-Type -TypeDefinition $code

$p1 = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\uuid:4d696e69-444c-164e-9d41-000000008fbe"
$p2 = "shell:::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\uuid:4d696e69-444c-164e-9d41-000000008fbe"
$p3 = "shell:MyComputerFolder"

Write-Output "Testing p1 (raw parsing name): $([ShellTester]::TestPath($p1))"
Write-Output "Testing p2 (with shell: prefix): $([ShellTester]::TestPath($p2))"
Write-Output "Testing p3 (shell:MyComputerFolder): $([ShellTester]::TestPath($p3))"
