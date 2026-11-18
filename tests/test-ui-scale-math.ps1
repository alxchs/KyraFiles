# Test runner for UiScaleMath (F090 layout compensation & drag region scaling)
# Run from repository root: powershell -NoProfile -File tests/test-ui-scale-math.ps1

$helperPath = Join-Path $PSScriptRoot "..\src\KyraFiles.App\Helpers\UI\UiScaleMath.cs"
if (-not (Test-Path $helperPath)) {
    Write-Error "Helper file not found at $helperPath"
    exit 1
}

$helperCode = Get-Content $helperPath -Raw

$cs = @"
$helperCode

public class UiScaleMathTests {
    private static bool Approx(double a, double b) {
        return System.Math.Abs(a - b) < 0.0001;
    }

    public static int RunAll() {
        int failed = 0;

        // 1. NormalizeScalePercent
        int[] normInputs  = new int[] { 80, 90, 100, 110, 125, 150, 0, -50, 50, 250 };
        int[] normExpect  = new int[] { 80, 90, 100, 110, 125, 150, 100, 100, 80, 150 };
        for (int i = 0; i < normInputs.Length; i++) {
            int got = KyraFiles.App.Helpers.UiScaleMath.NormalizeScalePercent(normInputs[i]);
            if (got != normExpect[i]) {
                Console.WriteLine(string.Format("[FAIL] NormalizeScalePercent({0}) = {1} (expected {2})", normInputs[i], got, normExpect[i]));
                failed++;
            } else {
                Console.WriteLine(string.Format("[PASS] NormalizeScalePercent({0}) = {1}", normInputs[i], got));
            }
        }

        // 2. ComputeCompensatedSize
        var c100 = KyraFiles.App.Helpers.UiScaleMath.ComputeCompensatedSize(1200, 800, 1.0);
        if (!c100.IsValid || !c100.IsIdentity || !double.IsNaN(c100.Width) || !double.IsNaN(c100.Height)) {
            Console.WriteLine("[FAIL] ComputeCompensatedSize(100%) should be identity NaN");
            failed++;
        } else {
            Console.WriteLine("[PASS] ComputeCompensatedSize(100%) -> identity NaN");
        }

        var c125 = KyraFiles.App.Helpers.UiScaleMath.ComputeCompensatedSize(1000, 800, 1.25);
        if (!c125.IsValid || c125.IsIdentity || !Approx(c125.Width, 800) || !Approx(c125.Height, 640)) {
            Console.WriteLine(string.Format("[FAIL] ComputeCompensatedSize(125%) = {0}x{1}", c125.Width, c125.Height));
            failed++;
        } else {
            Console.WriteLine(string.Format("[PASS] ComputeCompensatedSize(125%, 1000x800) -> {0}x{1}", c125.Width, c125.Height));
        }

        var c80 = KyraFiles.App.Helpers.UiScaleMath.ComputeCompensatedSize(1000, 800, 0.8);
        if (!c80.IsValid || c80.IsIdentity || !Approx(c80.Width, 1250) || !Approx(c80.Height, 1000)) {
            Console.WriteLine(string.Format("[FAIL] ComputeCompensatedSize(80%) = {0}x{1}", c80.Width, c80.Height));
            failed++;
        } else {
            Console.WriteLine(string.Format("[PASS] ComputeCompensatedSize(80%, 1000x800) -> {0}x{1}", c80.Width, c80.Height));
        }

        var c150 = KyraFiles.App.Helpers.UiScaleMath.ComputeCompensatedSize(1500, 900, 1.5);
        if (!c150.IsValid || c150.IsIdentity || !Approx(c150.Width, 1000) || !Approx(c150.Height, 600)) {
            Console.WriteLine(string.Format("[FAIL] ComputeCompensatedSize(150%) = {0}x{1}", c150.Width, c150.Height));
            failed++;
        } else {
            Console.WriteLine(string.Format("[PASS] ComputeCompensatedSize(150%, 1500x900) -> {0}x{1}", c150.Width, c150.Height));
        }

        var cZero = KyraFiles.App.Helpers.UiScaleMath.ComputeCompensatedSize(0, 800, 1.25);
        if (cZero.IsValid) {
            Console.WriteLine("[FAIL] ComputeCompensatedSize(0x800) should be invalid");
            failed++;
        } else {
            Console.WriteLine("[PASS] ComputeCompensatedSize(0x800) -> IsValid=false");
        }

        // 3. ScaleLocalRectToPhysical (AC-5 / AC-8: Windows DPI 1.5 + App 1.25)
        var localScaled = KyraFiles.App.Helpers.UiScaleMath.ScaleLocalRectToPhysical(10, 8, 200, 40, 1.5, 1.25);
        if (localScaled.X != 19 || localScaled.Y != 15 || localScaled.Width != 375 || localScaled.Height != 75) {
            Console.WriteLine(string.Format("[FAIL] ScaleLocalRectToPhysical = ({0},{1},{2},{3})", localScaled.X, localScaled.Y, localScaled.Width, localScaled.Height));
            failed++;
        } else {
            Console.WriteLine(string.Format("[PASS] ScaleLocalRectToPhysical(1.5x DPI, 1.25x UI) -> ({0},{1},{2},{3})", localScaled.X, localScaled.Y, localScaled.Width, localScaled.Height));
        }

        // 4. ScaleRootElementRectToPhysical (rootX/Y already UI-scaled, ActualSize unscaled)
        var elemScaled = KyraFiles.App.Helpers.UiScaleMath.ScaleRootElementRectToPhysical(12.5, 10.0, 32.0, 32.0, 1.5, 1.25);
        if (elemScaled.X != 19 || elemScaled.Y != 15 || elemScaled.Width != 60 || elemScaled.Height != 60) {
            Console.WriteLine(string.Format("[FAIL] ScaleRootElementRectToPhysical = ({0},{1},{2},{3})", elemScaled.X, elemScaled.Y, elemScaled.Width, elemScaled.Height));
            failed++;
        } else {
            Console.WriteLine(string.Format("[PASS] ScaleRootElementRectToPhysical(1.5x DPI, 1.25x UI) -> ({0},{1},{2},{3})", elemScaled.X, elemScaled.Y, elemScaled.Width, elemScaled.Height));
        }

        // 5. ComputeTitleBarRegions at 80% scale (Caption must span 100% of windowPixelWidth)
        var reg80 = KyraFiles.App.Helpers.UiScaleMath.ComputeTitleBarRegions(1600, 40, 5, 1.0, 0.8);
        if (reg80.CaptionWidth != 1600 || reg80.PhysicalTitleBarHeight != 32 || reg80.BorderWidth != 5 || reg80.RightBorderX != 1595) {
            Console.WriteLine(string.Format("[FAIL] ComputeTitleBarRegions(80%) = Caption={0}, H={1}, B={2}, RX={3}", reg80.CaptionWidth, reg80.PhysicalTitleBarHeight, reg80.BorderWidth, reg80.RightBorderX));
            failed++;
        } else {
            Console.WriteLine(string.Format("[PASS] ComputeTitleBarRegions(80%) -> Caption={0}, H={1}, RX={2}", reg80.CaptionWidth, reg80.PhysicalTitleBarHeight, reg80.RightBorderX));
        }

        // 6. ComputeTitleBarRegions at 150% UI + 125% DPI
        var reg150 = KyraFiles.App.Helpers.UiScaleMath.ComputeTitleBarRegions(1920, 40, 5, 1.25, 1.5);
        if (reg150.CaptionWidth != 1920 || reg150.PhysicalTitleBarHeight != 75 || reg150.BorderWidth != 7 || reg150.RightBorderX != 1913) {
            Console.WriteLine(string.Format("[FAIL] ComputeTitleBarRegions(150%+125%DPI) = Caption={0}, H={1}, B={2}, RX={3}", reg150.CaptionWidth, reg150.PhysicalTitleBarHeight, reg150.BorderWidth, reg150.RightBorderX));
            failed++;
        } else {
            Console.WriteLine(string.Format("[PASS] ComputeTitleBarRegions(150%+125%DPI) -> Caption={0}, H={1}, B={2}, RX={3}", reg150.CaptionWidth, reg150.PhysicalTitleBarHeight, reg150.BorderWidth, reg150.RightBorderX));
        }

        if (failed == 0) {
            Console.WriteLine("\nAll UiScaleMath test cases passed successfully.");
        }
        return failed;
    }
}
"@

Add-Type -TypeDefinition $cs -Language CSharp
$res = [UiScaleMathTests]::RunAll()
if ($res -ne 0) {
    Write-Error "Tests failed: $res failures."
    exit 1
} else {
    exit 0
}
