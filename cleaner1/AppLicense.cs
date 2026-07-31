namespace ModernFileCleaner;

public enum LicenseMode
{
    Demo,
    Full
}

/// <summary>Current license state, set once at startup (or via re-activation).</summary>
public static class AppLicense
{
    public static LicenseMode Mode { get; private set; } = LicenseMode.Demo;
    public static bool IsFullAccess => Mode == LicenseMode.Full;
    public static string? Username { get; private set; }
    public static string? Subscription { get; private set; }
    public static DateTime? ExpiryUtc { get; private set; }

    public static void SetFull(string? username, string? subscription, DateTime? expiryUtc)
    {
        Mode = LicenseMode.Full;
        Username = username;
        Subscription = subscription;
        ExpiryUtc = expiryUtc;
    }

    public static void SetDemo()
    {
        Mode = LicenseMode.Demo;
        Username = null;
        Subscription = null;
        ExpiryUtc = null;
    }
}
