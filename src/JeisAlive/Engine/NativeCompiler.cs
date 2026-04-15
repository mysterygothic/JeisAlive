using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace JeisAlive.Engine
{
    public static class NativeCompiler
    {
        private const int TimeoutMs = 120_000;

        public static byte[]? Compile(string cSource, Action<string>? log = null)
        {
            string? compilerPath = FindCompiler(out string compilerName);
            if (compilerPath == null)
            {
                log?.Invoke("[ERROR] No C compiler found. Bundled GCC missing and no gcc/cl in PATH.");
                return null;
            }

            log?.Invoke($"[*] Using compiler: {compilerName} ({compilerPath})");

            string tempDir = Path.Combine(Path.GetTempPath(), "jeisalive_" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            string inputPath = Path.Combine(tempDir, "stub.c");
            string outputPath = Path.Combine(tempDir, "stub.exe");

            try
            {
                File.WriteAllText(inputPath, cSource);
                log?.Invoke($"[*] Compiler: Source written to {inputPath} ({cSource.Length / 1024} KB)");

                // Save debug copy
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string debugCopy = Path.Combine(baseDir, "Output", "last_stub.c");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(debugCopy)!);
                    File.WriteAllText(debugCopy, cSource);
                }
                catch { }

                string args;
                if (compilerName == "gcc")
                {
                    // Build explicit -B and -L paths so ld.exe can find CRT objects and import libs
                    // without relying on GCC's relative path resolution (breaks on Parallels/network shares)
                    string? gccBinDir = Path.GetDirectoryName(compilerPath);
                    string gccBase = gccBinDir != null ? Path.GetFullPath(Path.Combine(gccBinDir, "..")) : "";
                    string gccLibDir = Path.Combine(gccBase, "lib", "gcc", "x86_64-w64-mingw32", "14.1.0");
                    string sysLibDir = Path.Combine(gccBase, "x86_64-w64-mingw32", "lib");

                    string libPaths = "";
                    if (Directory.Exists(gccLibDir))
                        libPaths += $" -B\"{gccLibDir}\"";
                    if (Directory.Exists(sysLibDir))
                        libPaths += $" -B\"{sysLibDir}\" -L\"{sysLibDir}\"";

                    args = $"-o \"{outputPath}\" \"{inputPath}\" -nostdinc{libPaths} -lkernel32 -luser32 -lbcrypt -lole32 -loleaut32 -ladvapi32 -lshell32 -mwindows -s";
                }
                else
                {
                    args = $"/Fe:\"{outputPath}\" \"{inputPath}\" /link kernel32.lib user32.lib bcrypt.lib ole32.lib oleaut32.lib advapi32.lib shell32.lib /SUBSYSTEM:WINDOWS";
                }

                log?.Invoke($"[*] Compiler: Compiling...");

                var psi = new ProcessStartInfo
                {
                    FileName = compilerPath,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = tempDir
                };

                // Add GCC's bin dir to PATH so it can find as.exe, ld.exe, collect2.exe
                string? gccDir = Path.GetDirectoryName(compilerPath);
                if (gccDir != null)
                {
                    string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                    psi.Environment["PATH"] = gccDir + ";" + currentPath;
                }

                using var process = new Process { StartInfo = psi };

                process.Start();

                // Read stdout and stderr in parallel to avoid pipe deadlock
                var stderrTask = process.StandardError.ReadToEndAsync();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();

                if (!process.WaitForExit(TimeoutMs))
                {
                    try { process.Kill(); } catch { }
                    log?.Invoke($"[ERROR] {compilerName}: Compilation timed out after {TimeoutMs / 1000}s.");
                    return null;
                }

                string stdout = stdoutTask.GetAwaiter().GetResult();
                string stderr = stderrTask.GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(stderr))
                    log?.Invoke($"[!] {compilerName} stderr:\n{stderr}");

                if (!string.IsNullOrWhiteSpace(stdout))
                    log?.Invoke($"[!] {compilerName} stdout:\n{stdout}");

                if (process.ExitCode != 0)
                {
                    log?.Invoke($"[ERROR] {compilerName}: Exit code {process.ExitCode}. Source saved to Output/last_stub.c for inspection.");
                    return null;
                }

                if (!File.Exists(outputPath))
                {
                    log?.Invoke($"[ERROR] {compilerName}: Output file not produced.");
                    return null;
                }

                byte[] result = File.ReadAllBytes(outputPath);
                log?.Invoke($"[+] {compilerName}: Compiled successfully ({result.Length / 1024} KB)");
                return result;
            }
            catch (Exception ex)
            {
                log?.Invoke($"[ERROR] Compiler exception: {ex.Message}");
                return null;
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static string? FindCompiler(out string name)
        {
            // 1. Check bundled GCC in Tools/w64devkit/bin/
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string bundledGcc = Path.Combine(baseDir, "Tools", "w64devkit", "bin", "gcc.exe");
            if (File.Exists(bundledGcc))
            {
                name = "gcc";
                return bundledGcc;
            }

            // 2. Check PATH for gcc
            string? path = FindInPath("gcc.exe");
            if (path != null)
            {
                name = "gcc";
                return path;
            }

            // 3. Fallback to MSVC cl.exe
            path = FindInPath("cl.exe");
            if (path != null)
            {
                name = "cl";
                return path;
            }

            name = "";
            return null;
        }

        private static string? FindInPath(string executable)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = executable,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(5000);

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    string firstLine = output.Split('\n')[0].Trim();
                    if (File.Exists(firstLine))
                        return firstLine;
                }
            }
            catch { }

            return null;
        }
    }
}
