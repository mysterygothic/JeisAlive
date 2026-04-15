using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace JeisAlive.Engine
{
    public static class CryptoEngine
    {
        private const int KeySize = 32;
        private const int IvSize = 16;
        private const int HmacSize = 32;
        private const int SaltSize = 32;

        // All layers use the same format: IV(16) || ciphertext || HMAC-SHA256(32)
        // Layer 3 also GZip compresses before encrypting (managed code decompresses)
        // Layers 1 and 2 do NOT GZip (native C stub decrypts — no GZip in C)

        public static (byte[] encrypted, byte[] key, byte[] iv) EncryptLayer3(byte[] payload)
        {
            byte[] compressed = GZipCompress(payload);
            byte[] key = GenerateRandom(KeySize);
            byte[] iv = GenerateRandom(IvSize);
            return (EncryptAesCbcHmac(compressed, key, iv), key, iv);
        }

        public static (byte[] encrypted, byte[] key, byte[] iv) EncryptLayer2(byte[] blob)
        {
            byte[] key = GenerateRandom(KeySize);
            byte[] iv = GenerateRandom(IvSize);
            return (EncryptAesCbcHmac(blob, key, iv), key, iv);
        }

        public static (byte[] encrypted, byte[] salt) EncryptLayer1(byte[] blob)
        {
            byte[] key = GenerateRandom(KeySize);
            byte[] salt = (byte[])key.Clone();

            byte[] iv = GenerateRandom(IvSize);
            byte[] encrypted = EncryptAesCbcHmac(blob, key, iv);

            // Output: salt(32) || encrypted (contains IV+ciphertext+HMAC inside)
            byte[] result = new byte[salt.Length + encrypted.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(encrypted, 0, result, salt.Length, encrypted.Length);

            return (result, salt);
        }

        public static byte[] PackLayer2Blob(
            byte[] l3Key, byte[] l3Iv, byte[] managedDll,
            List<(string name, byte[] data, byte action)> boundFiles,
            byte[] l3EncryptedPayload)
        {
            // New blob format:
            // L3Key(32) | L3IV(16) | ManagedDllLen(4) | ManagedDll
            // | BoundFileCount(4) | [NameLen(4)|Name|DataLen(4)|Data|Action(1)]×N
            // | PayloadLen(4) | Payload

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(l3Key);
            writer.Write(l3Iv);

            writer.Write(managedDll.Length);
            writer.Write(managedDll);

            int fileCount = boundFiles?.Count ?? 0;
            writer.Write(fileCount);

            if (boundFiles != null)
            {
                foreach (var (name, data, action) in boundFiles)
                {
                    byte[] nameBytes = Encoding.UTF8.GetBytes(name);
                    writer.Write(nameBytes.Length);
                    writer.Write(nameBytes);
                    writer.Write(data.Length);
                    writer.Write(data);
                    writer.Write(action);
                }
            }

            writer.Write(l3EncryptedPayload.Length);
            writer.Write(l3EncryptedPayload);

            writer.Flush();
            return ms.ToArray();
        }

        public static byte[] PackLayer1Input(byte[] l2Key, byte[] l2Iv, byte[] l2Encrypted)
        {
            byte[] result = new byte[l2Key.Length + l2Iv.Length + l2Encrypted.Length];
            Buffer.BlockCopy(l2Key, 0, result, 0, l2Key.Length);
            Buffer.BlockCopy(l2Iv, 0, result, l2Key.Length, l2Iv.Length);
            Buffer.BlockCopy(l2Encrypted, 0, result, l2Key.Length + l2Iv.Length, l2Encrypted.Length);
            return result;
        }

        // Format: IV(16) || AES-CBC ciphertext || HMAC-SHA256(32)
        private static byte[] EncryptAesCbcHmac(byte[] plaintext, byte[] key, byte[] iv)
        {
            byte[] ciphertext;
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using var enc = aes.CreateEncryptor();
                ciphertext = enc.TransformFinalBlock(plaintext, 0, plaintext.Length);
            }

            byte[] hmac;
            using (var h = new HMACSHA256(key))
            {
                h.TransformBlock(iv, 0, iv.Length, null, 0);
                h.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                hmac = h.Hash!;
            }

            byte[] result = new byte[IvSize + ciphertext.Length + HmacSize];
            Buffer.BlockCopy(iv, 0, result, 0, IvSize);
            Buffer.BlockCopy(ciphertext, 0, result, IvSize, ciphertext.Length);
            Buffer.BlockCopy(hmac, 0, result, IvSize + ciphertext.Length, HmacSize);
            return result;
        }

        private static byte[] GZipCompress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
                gzip.Write(data, 0, data.Length);
            return output.ToArray();
        }

        private static byte[] GenerateRandom(int length)
        {
            byte[] buffer = new byte[length];
            RandomNumberGenerator.Fill(buffer);
            return buffer;
        }
    }
}
