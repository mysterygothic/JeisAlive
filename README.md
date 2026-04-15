<p align="center">
  <img src="logo.ico" width="80" alt="JeisAlive">
</p>

# JeisAlive

.NET crypter and multi-file binder. Forked from [Jlaive](https://github.com/ch2sh/Jlaive) by ch2sh, rewritten from scratch.


---

## Features

- 3-layer AES-256-CBC encryption with HMAC-SHA256 per layer
- Native C loader compiled fresh each build via bundled GCC
- Multi-file binder (bind decoys, documents, other executables)
- Anti-debug, anti-VM, sandbox timing checks
- Self-delete (melt) after execution
- Output as native EXE or obfuscated batch file

---

## How It Works

![How It Works](howitworks.png)

Your payload gets encrypted through 3 nested AES layers. A native C stub is generated and compiled on the fly to unpack it at runtime. The stub hosts the .NET CLR to load a managed DLL that handles the final decryption and launches the original payload. Bound files get extracted and opened/executed alongside it.

---

## Scan Results

![VirusTotal Scan — April 15, 2026](VirusTotal-15-April-2026.png)

---

## Build

```
dotnet build JeisAlive.sln
```

Needs .NET 8 SDK + Windows. GCC is bundled, no extra installs.

---

## Usage

1. Select your payload (.exe)
2. Pick output format (EXE or BAT)
3. Toggle protection (Anti-Debug, Anti-VM, Melt)
4. Add files to binder if you want
5. Hit Build

---

## Credits

Forked from [Jlaive](https://github.com/ch2sh/Jlaive) by ch2sh. Rewritten by **gothyo**.

---

## Support

**BTC:** `bc1qv7z5grl02lyac0snwsqd0n5q3pjd0rxp7xwyxd`

**SOL:** `F3d2kDWAGvxXeH68Kx1D2ycQzxsHnjxfVVSFJmkKf4Ng`

**XMR:** `49WKmHs8r6vL3Wd1HD1FGfHWP9gGJobKmZWQ2U91upwTBj1AMo8SZrUWt8bTwCYzUzhhSo5zPYut2AdrRLbCofHX6ZEcP4z`

---
> **LEGAL DISCLAIMER — PLEASE READ!**
>
> I, the creator and all those associated with the development and production of this program are not responsible for any actions and or damages caused by this software. You bear the full responsibility of your actions and acknowledge that this software was created for educational purposes only. This software's intended purpose is NOT to be used maliciously, or on any system that you do not have own or have explicit permission to operate and use this program on. By using this software, you automatically agree to the above.

## License

MIT — see [LICENSE](LICENSE)
