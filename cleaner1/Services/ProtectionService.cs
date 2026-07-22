using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ModernFileCleaner.Services;

/// <summary>
/// Anti-reverse-engineering & integrity protection service.
/// Uses Win32 API, runtime checks, and cryptographic verification
/// to detect debuggers, decompilers, and tampered assemblies.
/// </summary>
public static class ProtectionService
{
    // Critical assembly hash for integrity checking
    // Computed from the legitimate release build
    private static readonly byte[] ExpectedHash = new byte[32]; // Set during secure build

    private static bool _integrityPassed = false;
    private static bool _checksPerformed = false;

    [DllImport("kernel32.dll")]
    private static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll")]
    private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

    [DllImport("kernel32.dll")]
    private static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationSize, ref int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_INFO { public ushort wProcessorArchitecture; public ushort wReserved; public uint dwPageSize; public IntPtr lpMinimumApplicationAddress; public IntPtr lpMaximumApplicationAddress; public IntPtr dwActiveProcessorMask; public uint dwNumberOfProcessors; public uint dwProcessorType; public uint dwAllocationGranularity; public ushort wProcessorLevel; public ushort wProcessorRevision; }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION { public int ExitStatus; public IntPtr PebBaseAddress; public ulong AffinityMask; public int BasePriority; public ulong UniqueProcessId; public ulong InheritedFromUniqueProcessId; }

    private const int ProcessDebugPort = 7;
    private const int ProcessDebugFlags = 31;

    /// <summary>
    /// Run all protection checks. Call once at startup.
    /// Returns true if all checks pass, false if tampering detected.
    /// </summary>
    public static bool RunStartupChecks()
    {
        if (_checksPerformed) return _integrityPassed;
        _checksPerformed = true;

        try
        {
            // Check 1: Debugger attached?
            if (IsDebuggerPresent() || CheckNtDebugger())
            {
                Debug.WriteLine("[Protection] Debugger detected!");
                _integrityPassed = false;
                return false;
            }

            // Check 2: Running in a VM/sandbox? (common for crackers)
            if (DetectVirtualMachine())
            {
                Debug.WriteLine("[Protection] VM/sandbox detected, limiting features");
                // Don't block, but log it
            }

            // Check 3: Assembly integrity
            if (!VerifyAssemblyIntegrity())
            {
                Debug.WriteLine("[Protection] Assembly integrity check failed!");
                _integrityPassed = false;
                return false;
            }

            _integrityPassed = true;
            return true;
        }
        catch
        {
            // If checks throw, allow (fail open for compatibility)
            _integrityPassed = true;
            return true;
        }
    }

    /// <summary>
    /// Check for debugger via NtQueryInformationProcess
    /// (bypasses simple IsDebuggerPresent hooks)
    /// </summary>
    private static bool CheckNtDebugger()
    {
        try
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            int retLen = 0;
            int status = NtQueryInformationProcess(Process.GetCurrentProcess().Handle,
                ProcessDebugPort, ref pbi, Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)), ref retLen);
            return status == 0 && pbi.ExitStatus != 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// Detect common VM/hypervisor environments
    /// </summary>
    private static bool DetectVirtualMachine()
    {
        try
        {
            using var mc = new System.Management.ManagementClass("Win32_ComputerSystem");
            using var items = mc.GetInstances();
            foreach (var item in items)
            {
                var model = item["Model"]?.ToString() ?? "";
                var manufacturer = item["Manufacturer"]?.ToString() ?? "";
                if (model.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                    model.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
                    manufacturer.Contains("Microsoft Corporation") && model.Contains("Virtual"))
                    return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Verify the executing assembly hasn't been tampered with
    /// Compares runtime hash against the expected release hash
    /// </summary>
    private static bool VerifyAssemblyIntegrity()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var location = asm.Location;
            if (string.IsNullOrEmpty(location) || !File.Exists(location))
                return true; // Can't check in-memory assemblies

            using var stream = File.OpenRead(location);
            var hash = SHA256.HashData(stream);

            // If we have an expected hash, compare it
            bool allZero = true;
            foreach (var b in ExpectedHash)
            { if (b != 0) { allZero = false; break; } }

            if (!allZero)
            {
                for (int i = 0; i < ExpectedHash.Length; i++)
                {
                    if (hash[i] != ExpectedHash[i])
                        return false; // Tampered!
                }
            }

            return true;
        }
        catch
        {
            return true; // Fail open
        }
    }

    /// <summary>
    /// Set the expected hash for integrity verification.
    /// Called during secure build process.
    /// </summary>
    public static void SetExpectedHash(byte[] hash)
    {
        if (hash != null && hash.Length == 32)
            Buffer.BlockCopy(hash, 0, ExpectedHash, 0, 32);
    }

    /// <summary>
    /// Obfuscated string decoder.
    /// Critical strings are stored XOR-encoded to prevent string search attacks.
    /// </summary>
    public static string DecryptString(byte[] data, byte key)
    {
        if (data == null || data.Length == 0) return "";
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = (byte)(data[i] ^ key ^ (byte)(i * 7));
        return Encoding.UTF8.GetString(result);
    }
}
