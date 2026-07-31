using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ModernFileCleaner.Services;

/// <summary>
/// DPAPI-protected storage for the license key string.
/// Only the key is stored (no HWID, no type prefix) so the app can
/// re-attempt online login at next startup. Offline use is not supported.
/// </summary>
public static class LicenseStorage
{
    private const string FileName = "license.key";

    public static string? Load()
    {
        try
        {
            if (!File.Exists(FileName)) return null;
            var enc = Convert.FromBase64String(File.ReadAllText(FileName));
            var dec = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dec);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(string key)
    {
        try
        {
            var enc = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser);
            File.WriteAllText(FileName, Convert.ToBase64String(enc));
        }
        catch { }
    }

    public static void Delete()
    {
        try { if (File.Exists(FileName)) File.Delete(FileName); } catch { }
    }
}
