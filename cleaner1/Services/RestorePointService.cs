using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ModernFileCleaner.Services;

public static class RestorePointService
{
    [DllImport("srclient.dll")]
    private static extern int SRSetRestorePointW(ref RESTOREPOINTINFO pRestorePtSpec, out STATEMGRSTATUS pMgrStatus);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RESTOREPOINTINFO
    {
        public int dwEventType;
        public int dwRestorePtType;
        public long llSequenceNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STATEMGRSTATUS
    {
        public int nStatus;
        public long llSequenceNumber;
    }

    private const int BEGIN_SYSTEM_CHANGE = 100;
    private const int END_SYSTEM_CHANGE = 101;
    private const int MODIFY_SETTINGS = 10;
    private const int APPLICATION_INSTALL = 0;

    /// <summary>
    /// Creates a system restore point before making system changes.
    /// Returns true if successful, false otherwise.
    /// </summary>
    public static bool CreateRestorePoint(string description = "System Sweep - Pre-optimization restore point")
    {
        try
        {
            // Check if System Restore is available
            if (!IsSystemRestoreEnabled())
            {
                Debug.WriteLine("[RestorePoint] System Restore is not enabled on this system");
                return false;
            }

            var restoreInfo = new RESTOREPOINTINFO
            {
                dwEventType = BEGIN_SYSTEM_CHANGE,
                dwRestorePtType = MODIFY_SETTINGS,
                llSequenceNumber = 0,
                szDescription = description
            };

            int result = SRSetRestorePointW(ref restoreInfo, out var status);
            if (result == 0)
            {
                Debug.WriteLine($"[RestorePoint] Failed with status: {status.nStatus}");
                return false;
            }

            Debug.WriteLine($"[RestorePoint] Created successfully: {description}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RestorePoint] Error: {ex.Message}");
            return false;
        }
    }

    public static bool IsSystemRestoreEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore");
            if (key == null) return false;
            var rp = key.GetValue("RPSessionInterval");
            return rp != null;
        }
        catch { return false; }
    }
}
