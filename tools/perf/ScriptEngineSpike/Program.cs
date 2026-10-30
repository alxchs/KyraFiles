using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Jint;
using Jint.Runtime;
using MoonSharp.Interpreter;

namespace ScriptEngineSpike;

public record TestResult(
    string Engine,
    string Criterion,
    string TestName,
    string Status, // PASS / FAIL
    double DurationMs,
    long PeakMemoryMb,
    string Details
);

public class SafeFs
{
    private readonly List<string> _files = new() { "app.log", "error.log", "data.json", "config.ini", "archive.zip" };

    public string[] list(string path)
    {
        return _files.ToArray();
    }

    public string[] filter(string pattern)
    {
        return _files.Where(f => f.EndsWith(pattern, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}

public class SafeLog
{
    public List<string> Logs { get; } = new();

    public void info(string message)
    {
        Logs.Add($"[INFO] {message}");
    }

    public void warn(string message)
    {
        Logs.Add($"[WARN] {message}");
    }
}

public class Program
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ".git")) || Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    public static int Main(string[] args)
    {
        Console.WriteLine("===============================================================");
        Console.WriteLine(" ScriptEngineSpike: MoonSharp (Lua) vs Jint (JavaScript)");
        Console.WriteLine("===============================================================");

        var results = new List<TestResult>();

        // 1. Run MoonSharp tests
        RunMoonSharpTests(results);

        // 2. Run Jint tests
        RunJintTests(results);

        // 3. Print Summary Table
        Console.WriteLine("\n======================= RESULT SUMMARY =======================");
        Console.WriteLine($"{"Engine",-10} | {"Criterion",-10} | {"TestName",-30} | {"Status",-6} | {"Time(ms)",-10} | {"PeakMem(MB)",-12} | Details");
        Console.WriteLine(new string('-', 110));

        foreach (var r in results)
        {
            Console.WriteLine($"{r.Engine,-10} | {r.Criterion,-10} | {r.TestName,-30} | {r.Status,-6} | {r.DurationMs,10:F2} | {r.PeakMemoryMb,12} | {r.Details}");
        }

        // 4. Output CSV to disk in docs/agents/evidence/F060
        string repoRoot = FindRepoRoot();
        string csvDir = Path.Combine(repoRoot, "docs", "agents", "evidence", "F060");
        Directory.CreateDirectory(csvDir);
        string csvTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string csvPath = Path.Combine(csvDir, $"script_engine_spike_{csvTimestamp}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("Engine,Criterion,TestName,Status,DurationMs,PeakMemoryMb,Details");
        foreach (var r in results)
        {
            string safeDetails = "\"" + r.Details.Replace("\"", "\"\"") + "\"";
            sb.AppendLine($"{r.Engine},{r.Criterion},{r.TestName},{r.Status},{r.DurationMs.ToString("F2", CultureInfo.InvariantCulture)},{r.PeakMemoryMb},{safeDetails}");
        }

        File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"\n[OK] CSV evidence written to: {Path.GetFullPath(csvPath)}");

        return 0;
    }

    private static void RunMoonSharpTests(List<TestResult> results)
    {
        Console.WriteLine("\n--- Running MoonSharp (Lua) Suite ---");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        long initialMem = Process.GetCurrentProcess().WorkingSet64;

        // (a) Trivial script load and run
        {
            var sw = Stopwatch.StartNew();
            // Use Preset_HardSandbox for security
            var script = new Script(CoreModules.Preset_HardSandbox);
            var res = script.DoString("local a = 10; local b = 32; return a + b;");
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            bool pass = res.Type == DataType.Number && res.Number == 42;
            results.Add(new TestResult("MoonSharp", "Trivial", "LoadAndRunTrivial", pass ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, $"Result: {res.Number}"));
            Console.WriteLine($"[MoonSharp] (a) Trivial: {sw.Elapsed.TotalMilliseconds:F2} ms (Result={res.Number})");
        }

        // (b) Timeout: Infinite loop interrupted in <= 2s
        {
            var sw = Stopwatch.StartNew();
            var script = new Script(CoreModules.Preset_HardSandbox);
            var fn = script.LoadString("while true do local x = 1 end");
            var coro = script.CreateCoroutine(fn);
            // In MoonSharp, preemptive interruption requires AutoYieldCounter
            coro.Coroutine.AutoYieldCounter = 1000;
            var maxDuration = TimeSpan.FromSeconds(2);
            bool interrupted = false;
            string details = "";
            int resumeCount = 0;

            while (coro.Coroutine.State == CoroutineState.Suspended || 
                   coro.Coroutine.State == CoroutineState.ForceSuspended || 
                   coro.Coroutine.State == CoroutineState.NotStarted)
            {
                if (sw.Elapsed >= maxDuration)
                {
                    interrupted = true;
                    details = $"Interrupted by host timer at {sw.Elapsed.TotalSeconds:F2}s via AutoYieldCounter ({resumeCount} yields)";
                    break;
                }
                var yieldVal = coro.Coroutine.Resume();
                resumeCount++;
            }
            if (!interrupted)
            {
                details = $"Loop terminated prematurely! State={coro.Coroutine.State}, Resumes={resumeCount}";
            }
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            bool pass = interrupted && sw.Elapsed.TotalSeconds <= 2.2; // within 2s tolerance
            results.Add(new TestResult("MoonSharp", "Timeout", "InfiniteLoopTimeout", pass ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, details));
            Console.WriteLine($"[MoonSharp] (b) Timeout: {details} (Elapsed={sw.Elapsed.TotalMilliseconds:F2} ms)");
        }

        // (c) Memory / Instruction Limit
        {
            // MoonSharp DOES NOT provide a native LimitMemory option.
            // It provides AutoYieldCounter instruction stepping.
            var sw = Stopwatch.StartNew();
            var script = new Script(CoreModules.Preset_HardSandbox);
            var fn = script.LoadString("local t = {}; for i = 1, 1000000 do t[i] = i end; return #t;");
            var coro = script.CreateCoroutine(fn);
            coro.Coroutine.AutoYieldCounter = 1000;

            int maxCycles = 50; // Instruction cycle limit (~50k instructions)
            int cycles = 0;
            bool stoppedByLimit = false;

            while (coro.Coroutine.State == CoroutineState.Suspended || 
                   coro.Coroutine.State == CoroutineState.ForceSuspended || 
                   coro.Coroutine.State == CoroutineState.NotStarted)
            {
                cycles++;
                if (cycles > maxCycles)
                {
                    stoppedByLimit = true;
                    break;
                }
                coro.Coroutine.Resume();
            }
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            // Record finding: No native memory limit; instruction limit possible via manual coroutine yield counting
            results.Add(new TestResult("MoonSharp", "Limit", "InstructionLimitCoro", stoppedByLimit ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, $"Instruction limit enforced via AutoYield ({cycles} cycles). No native memory limit."));
            Console.WriteLine($"[MoonSharp] (c) Limit: Instruction limit reached at {cycles} cycles. No native memory limit.");
        }

        // (d) API Restrita & Escape Tests
        {
            UserData.RegisterType<SafeFs>();
            UserData.RegisterType<SafeLog>();

            var script = new Script(CoreModules.Preset_HardSandbox);
            var safeFs = new SafeFs();
            var safeLog = new SafeLog();
            script.Globals["fs"] = safeFs;
            script.Globals["log"] = safeLog;

            // Legitimate usage
            var sw = Stopwatch.StartNew();
            var legitRes = script.DoString(@"
                local items = fs:filter('.log')
                for i = 1, #items do
                    log:info('Processing ' .. items[i])
                end
                return #items
            ");
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            bool legitPass = legitRes.Number == 2 && safeLog.Logs.Count == 2;
            results.Add(new TestResult("MoonSharp", "API", "ExposedApiNormalUse", legitPass ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, $"Exposed fs and log worked. Logs count: {safeLog.Logs.Count}"));
            Console.WriteLine($"[MoonSharp] (d) API Normal: {safeLog.Logs.Count} logs registered.");

            // Fuga 1: os.execute
            sw.Restart();
            bool fugaOsBlocked = false;
            string fugaOsMsg = "";
            try
            {
                var r = script.DoString("return os.execute('cmd.exe /c echo pwned')");
                fugaOsMsg = $"Returned: {r}";
            }
            catch (Exception ex)
            {
                fugaOsBlocked = true;
                fugaOsMsg = $"Blocked with exception: {ex.GetType().Name} - {ex.Message}";
            }
            // In HardSandbox, os table only has time/clock functions, so os.execute is nil
            if (!fugaOsBlocked)
            {
                // Check if os is nil or os.execute was nil
                var check = script.DoString("return os and os.execute ~= nil");
                if (check.Boolean == false)
                {
                    fugaOsBlocked = true;
                    fugaOsMsg = "os.execute is nil (blocked by Preset_HardSandbox)";
                }
            }
            sw.Stop();
            results.Add(new TestResult("MoonSharp", "Escape", "Fuga_os_execute", fugaOsBlocked ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, fugaOsMsg));
            Console.WriteLine($"[MoonSharp] (d) Fuga os.execute: {(fugaOsBlocked ? "BLOCKED" : "ALLOWED")} ({fugaOsMsg})");

            // Fuga 2: io.open
            sw.Restart();
            bool fugaIoBlocked = false;
            string fugaIoMsg = "";
            try
            {
                var r = script.DoString("return io.open('pwned.txt', 'w')");
                fugaIoMsg = $"Returned: {r}";
            }
            catch (Exception ex)
            {
                fugaIoBlocked = true;
                fugaIoMsg = $"Blocked with exception: {ex.GetType().Name} - {ex.Message}";
            }
            if (!fugaIoBlocked)
            {
                var check = script.DoString("return io ~= nil");
                if (check.Boolean == false)
                {
                    fugaIoBlocked = true;
                    fugaIoMsg = "io table is nil (blocked by Preset_HardSandbox)";
                }
            }
            sw.Stop();
            results.Add(new TestResult("MoonSharp", "Escape", "Fuga_io_open", fugaIoBlocked ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, fugaIoMsg));
            Console.WriteLine($"[MoonSharp] (d) Fuga io.open: {(fugaIoBlocked ? "BLOCKED" : "ALLOWED")} ({fugaIoMsg})");

            // Fuga 3: System.IO / CLR reflection
            sw.Restart();
            bool fugaClrBlocked = false;
            string fugaClrMsg = "";
            try
            {
                var r = script.DoString("local f = System.IO.File; return f");
                fugaClrMsg = $"Returned: {r}";
            }
            catch (Exception ex)
            {
                fugaClrBlocked = true;
                fugaClrMsg = $"Blocked: {ex.GetType().Name} - {ex.Message}";
            }
            if (!fugaClrBlocked)
            {
                var check = script.DoString("return System ~= nil");
                if (check.Boolean == false)
                {
                    fugaClrBlocked = true;
                    fugaClrMsg = "System symbol is nil/undefined in Lua";
                }
            }
            sw.Stop();
            results.Add(new TestResult("MoonSharp", "Escape", "Fuga_System_IO", fugaClrBlocked ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, fugaClrMsg));
            Console.WriteLine($"[MoonSharp] (d) Fuga System.IO: {(fugaClrBlocked ? "BLOCKED" : "ALLOWED")} ({fugaClrMsg})");

            // Fuga 4: Process.Start
            sw.Restart();
            bool fugaProcessBlocked = false;
            string fugaProcessMsg = "";
            try
            {
                var r = script.DoString("local p = System.Diagnostics.Process.Start('notepad.exe'); return p");
                fugaProcessMsg = $"Returned: {r}";
            }
            catch (Exception ex)
            {
                fugaProcessBlocked = true;
                fugaProcessMsg = $"Blocked: {ex.GetType().Name} - {ex.Message}";
            }
            if (!fugaProcessBlocked)
            {
                var check = script.DoString("return Process ~= nil");
                if (check.Boolean == false)
                {
                    fugaProcessBlocked = true;
                    fugaProcessMsg = "Process symbol is nil/undefined in Lua";
                }
            }
            sw.Stop();
            results.Add(new TestResult("MoonSharp", "Escape", "Fuga_Process_Start", fugaProcessBlocked ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, fugaProcessMsg));
            Console.WriteLine($"[MoonSharp] (d) Fuga Process.Start: {(fugaProcessBlocked ? "BLOCKED" : "ALLOWED")} ({fugaProcessMsg})");
        }

        // (e) Peak Memory
        {
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            results.Add(new TestResult("MoonSharp", "MemoryPeak", "ProcessMemoryPeak", "PASS", 0, peak, $"Peak working set: {peak} MB"));
            Console.WriteLine($"[MoonSharp] (e) Peak Memory: {peak} MB");
        }
    }

    private static void RunJintTests(List<TestResult> results)
    {
        Console.WriteLine("\n--- Running Jint (JavaScript) Suite ---");
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // (a) Trivial script load and run
        {
            var sw = Stopwatch.StartNew();
            using var engine = new Engine();
            var val = engine.Evaluate("let a = 10; let b = 32; a + b;");
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            bool pass = val.IsNumber() && val.AsNumber() == 42;
            results.Add(new TestResult("Jint", "Trivial", "LoadAndRunTrivial", pass ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, $"Result: {val.AsNumber()}"));
            Console.WriteLine($"[Jint] (a) Trivial: {sw.Elapsed.TotalMilliseconds:F2} ms (Result={val.AsNumber()})");
        }

        // (b) Timeout: Infinite loop interrupted in <= 2s
        {
            var sw = Stopwatch.StartNew();
            bool timedOut = false;
            string details = "";
            try
            {
                using var engine = new Engine(options =>
                {
                    options.TimeoutInterval(TimeSpan.FromSeconds(2));
                });
                engine.Execute("while (true) {}");
            }
            catch (TimeoutException ex)
            {
                timedOut = true;
                details = $"Interrupted by native TimeoutInterval: {ex.Message}";
            }
            catch (Exception ex)
            {
                details = $"Interrupted by exception: {ex.GetType().Name} - {ex.Message}";
            }
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            bool pass = timedOut && sw.Elapsed.TotalSeconds <= 2.2;
            results.Add(new TestResult("Jint", "Timeout", "InfiniteLoopTimeout", pass ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, details));
            Console.WriteLine($"[Jint] (b) Timeout: {details} (Elapsed={sw.Elapsed.TotalMilliseconds:F2} ms)");
        }

        // (b.2) Timeout via CancellationToken
        {
            var sw = Stopwatch.StartNew();
            bool cancelled = false;
            string details = "";
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            try
            {
                using var engine = new Engine(options =>
                {
                    options.CancellationToken(cts.Token);
                });
                engine.Execute("while (true) {}");
            }
            catch (ExecutionCanceledException ex)
            {
                cancelled = true;
                details = $"Interrupted by CancellationToken (500ms): {ex.Message}";
            }
            catch (OperationCanceledException ex)
            {
                cancelled = true;
                details = $"Interrupted by OperationCanceledException: {ex.Message}";
            }
            catch (Exception ex)
            {
                details = $"Exception: {ex.GetType().Name} - {ex.Message}";
            }
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            bool pass = cancelled && sw.Elapsed.TotalMilliseconds <= 1000;
            results.Add(new TestResult("Jint", "Timeout", "CancellationToken500ms", pass ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, details));
            Console.WriteLine($"[Jint] (b.2) CancellationToken: {details} (Elapsed={sw.Elapsed.TotalMilliseconds:F2} ms)");
        }

        // (c) Memory Limit
        {
            var sw = Stopwatch.StartNew();
            bool memExceeded = false;
            string details = "";
            try
            {
                using var engine = new Engine(options =>
                {
                    // Limit to 2 MB
                    options.LimitMemory(2_000_000);
                });
                // Script that allocates arrays rapidly
                engine.Execute(@"
                    const arr = [];
                    while (true) {
                        arr.push(new Array(10000).fill('allocating memory block...'));
                    }
                ");
            }
            catch (MemoryLimitExceededException ex)
            {
                memExceeded = true;
                details = $"MemoryLimitExceededException caught: {ex.Message}";
            }
            catch (Exception ex)
            {
                details = $"Exception: {ex.GetType().Name} - {ex.Message}";
            }
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            results.Add(new TestResult("Jint", "Limit", "MemoryLimit2MB", memExceeded ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, details));
            Console.WriteLine($"[Jint] (c) Memory Limit: {(memExceeded ? "PASS" : "FAIL")} ({details})");
        }

        // (c.2) MaxStatements limit
        {
            var sw = Stopwatch.StartNew();
            bool statementLimit = false;
            string details = "";
            try
            {
                using var engine = new Engine(options =>
                {
                    options.MaxStatements(500);
                });
                engine.Execute("let count = 0; while (true) { count++; }");
            }
            catch (StatementsCountOverflowException ex)
            {
                statementLimit = true;
                details = $"StatementsCountOverflowException: {ex.Message}";
            }
            catch (Exception ex)
            {
                details = $"Exception: {ex.GetType().Name} - {ex.Message}";
            }
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            results.Add(new TestResult("Jint", "Limit", "MaxStatements500", statementLimit ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, details));
            Console.WriteLine($"[Jint] (c.2) Statements Limit: {(statementLimit ? "PASS" : "FAIL")} ({details})");
        }

        // (d) API Restrita & Escape Tests
        {
            var safeFs = new SafeFs();
            var safeLog = new SafeLog();

            using var engine = new Engine(options =>
            {
                // Note: Do NOT call options.AllowClr()!
                options.TimeoutInterval(TimeSpan.FromSeconds(2));
            });

            engine.SetValue("fs", safeFs);
            engine.SetValue("log", safeLog);

            // Legitimate usage
            var sw = Stopwatch.StartNew();
            engine.Execute(@"
                const files = fs.filter('.log');
                for (let i = 0; i < files.length; i++) {
                    log.info('Processing ' + files[i]);
                }
            ");
            sw.Stop();
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            bool legitPass = safeLog.Logs.Count == 2;
            results.Add(new TestResult("Jint", "API", "ExposedApiNormalUse", legitPass ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, $"Exposed fs and log worked. Logs count: {safeLog.Logs.Count}"));
            Console.WriteLine($"[Jint] (d) API Normal: {safeLog.Logs.Count} logs registered.");

            // Fuga 1: System.IO.File
            sw.Restart();
            bool fugaIoBlocked = false;
            string fugaIoMsg = "";
            try
            {
                engine.Execute("System.IO.File.WriteAllText('pwned.txt', 'evil');");
                fugaIoMsg = "Allowed! (System.IO executed)";
            }
            catch (JavaScriptException ex)
            {
                fugaIoBlocked = true;
                fugaIoMsg = $"Blocked with JavaScriptException: {ex.Message}";
            }
            catch (Exception ex)
            {
                fugaIoBlocked = true;
                fugaIoMsg = $"Blocked: {ex.GetType().Name} - {ex.Message}";
            }
            sw.Stop();
            results.Add(new TestResult("Jint", "Escape", "Fuga_System_IO_File", fugaIoBlocked ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, fugaIoMsg));
            Console.WriteLine($"[Jint] (d) Fuga System.IO.File: {(fugaIoBlocked ? "BLOCKED" : "ALLOWED")} ({fugaIoMsg})");

            // Fuga 2: System.Diagnostics.Process
            sw.Restart();
            bool fugaProcBlocked = false;
            string fugaProcMsg = "";
            try
            {
                engine.Execute("System.Diagnostics.Process.Start('notepad.exe');");
                fugaProcMsg = "Allowed! (Process.Start executed)";
            }
            catch (JavaScriptException ex)
            {
                fugaProcBlocked = true;
                fugaProcMsg = $"Blocked with JavaScriptException: {ex.Message}";
            }
            catch (Exception ex)
            {
                fugaProcBlocked = true;
                fugaProcMsg = $"Blocked: {ex.GetType().Name} - {ex.Message}";
            }
            sw.Stop();
            results.Add(new TestResult("Jint", "Escape", "Fuga_Process_Start", fugaProcBlocked ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, fugaProcMsg));
            Console.WriteLine($"[Jint] (d) Fuga Process.Start: {(fugaProcBlocked ? "BLOCKED" : "ALLOWED")} ({fugaProcMsg})");

            // Fuga 3: require
            sw.Restart();
            bool fugaReqBlocked = false;
            string fugaReqMsg = "";
            try
            {
                engine.Execute("const fs2 = require('fs');");
                fugaReqMsg = "Allowed! (require resolved)";
            }
            catch (JavaScriptException ex)
            {
                fugaReqBlocked = true;
                fugaReqMsg = $"Blocked: {ex.Message}";
            }
            catch (Exception ex)
            {
                fugaReqBlocked = true;
                fugaReqMsg = $"Blocked: {ex.GetType().Name} - {ex.Message}";
            }
            sw.Stop();
            results.Add(new TestResult("Jint", "Escape", "Fuga_require", fugaReqBlocked ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, fugaReqMsg));
            Console.WriteLine($"[Jint] (d) Fuga require: {(fugaReqBlocked ? "BLOCKED" : "ALLOWED")} ({fugaReqMsg})");

            // Fuga 4: import
            sw.Restart();
            bool fugaImpBlocked = false;
            string fugaImpMsg = "";
            try
            {
                engine.Execute("import('fs');");
                fugaImpMsg = "Allowed! (import resolved)";
            }
            catch (JavaScriptException ex)
            {
                fugaImpBlocked = true;
                fugaImpMsg = $"Blocked: {ex.Message}";
            }
            catch (Exception ex)
            {
                fugaImpBlocked = true;
                fugaImpMsg = $"Blocked: {ex.GetType().Name} - {ex.Message}";
            }
            sw.Stop();
            results.Add(new TestResult("Jint", "Escape", "Fuga_import", fugaImpBlocked ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, fugaImpMsg));
            Console.WriteLine($"[Jint] (d) Fuga import: {(fugaImpBlocked ? "BLOCKED" : "ALLOWED")} ({fugaImpMsg})");

            // Fuga 5: Prototype / Function constructor reflection escape
            sw.Restart();
            bool fugaProtoBlocked = false;
            string fugaProtoMsg = "";
            try
            {
                var protoRes = engine.Evaluate("this.constructor.constructor('return typeof process')()");
                fugaProtoMsg = $"Returned: {protoRes}";
                if (protoRes.AsString() == "undefined")
                {
                    fugaProtoBlocked = true;
                    fugaProtoMsg = "Function constructor cannot access host process (undefined)";
                }
            }
            catch (Exception ex)
            {
                fugaProtoBlocked = true;
                fugaProtoMsg = $"Blocked: {ex.GetType().Name} - {ex.Message}";
            }
            sw.Stop();
            results.Add(new TestResult("Jint", "Escape", "Fuga_Constructor_Escape", fugaProtoBlocked ? "PASS" : "FAIL", sw.Elapsed.TotalMilliseconds, peak, fugaProtoMsg));
            Console.WriteLine($"[Jint] (d) Fuga Prototype/Constructor: {(fugaProtoBlocked ? "BLOCKED" : "ALLOWED")} ({fugaProtoMsg})");
        }

        // (e) Peak Memory
        {
            long peak = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            results.Add(new TestResult("Jint", "MemoryPeak", "ProcessMemoryPeak", "PASS", 0, peak, $"Peak working set: {peak} MB"));
            Console.WriteLine($"[Jint] (e) Peak Memory: {peak} MB");
        }
    }
}
