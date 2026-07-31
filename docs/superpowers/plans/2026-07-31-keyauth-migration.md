# KeyAuth-Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** SystemSweep von RSA-2048-Offline-Lizenz + Keyzy auf KeyAuth (v1.3, session-basiert, online-only) umstellen, inkl. Demo-Modus-Feature-Gating und sicherem `.env`-Credential-Handling.

**Architecture:** Neuer `KeyAuthService` (HttpClient → `https://keyauth.win/api/1.3/`) mit Ed25519-Response-Verifikation (vendoriertes `Ed25519.cs` aus offizieller KeyAuth-SDK). Zustand in statischem `AppLicense` (Full/Demo). Credentials via `.env`-Parser (`AppEnv`) statt Hardcoding. DPAPI-`license.key` speichert nur noch den Key-String für Auto-Re-Login. Demo-Modus: alle systemverändernden Aktionen gesperrt.

**Tech Stack:** .NET 8, WPF (`net8.0-windows`), WPF-UI 3.x, Newtonsoft.Json (vorhanden), `System.Security.Cryptography.ProtectedData` (vorhanden), kein neues NuGet-Paket.

## Global Constraints

- `.env` wird NIE committet; `.env.example` wird committet (Platzhalter).
- KeyAuth-Credentials erscheinen nirgends im Quellcode — nur in `.env`.
- Kein `Co-Authored-By`-Trailer in Commit-Messages (Nutzer-Wunsch).
- Nur-Online-Modell: kein Offline-Cache, kein Offline-Grace.
- Ed25519-Verifikation jeder KeyAuth-Response, fail-closed.
- Demo-Modus (keine Voll-Lizenz) → keine systemverändernden Aktionen (Clean, Tweaks, Duplikate löschen, Cache leeren, Startup ändern, Restore-Punkt).
- Alle Aufgaben enden mit erfolgreichem `dotnet build cleaner1/cleaner1.csproj` und einem Commit.

---

### Task 1: `.env`-Infrastruktur + `AppEnv`

**Files:**
- Create: `cleaner1/Services/AppEnv.cs`
- Create: `.env.example`
- Create: `.env` (gitignored — echte Werte, wird nicht committet)
- Modify: `.gitignore`

**Interfaces:**
- Consumes: — (frische Basis)
- Produces: `AppEnv.EnsureLoaded()` (void), `AppEnv.Get(key, fallback="")` → `string`

- [ ] **Step 1: `.env.example` anlegen**

`.env.example` (commit-tauglich, Platzhalter):
```
# KeyAuth application credentials — https://keyauth.cc
# Copy this file to .env and fill in the real values.
# Never commit the .env file.
KEYAUTH_NAME=System sweep
KEYAUTH_OWNERID=your_ownerid_here
KEYAUTH_SECRET=your_secret_here
KEYAUTH_VERSION=1.0
```

- [ ] **Step 2: `.env` anlegen (lokale echte Werte, gitignored)**

`.env` (echte Werte aus KeyAuth-Dashboard, wird von git ignoriert):
```
KEYAUTH_NAME=System sweep
KEYAUTH_OWNERID=<ownerid aus KeyAuth-Dashboard>
KEYAUTH_SECRET=<secret aus KeyAuth-Dashboard>
KEYAUTH_VERSION=1.0
```

- [ ] **Step 3: `.gitignore` erweitern**

Füge ans Ende von `.gitignore` hinzu:
```
# KeyAuth credentials
.env
```

- [ ] **Step 4: `AppEnv.cs` schreiben**

`cleaner1/Services/AppEnv.cs`:
```csharp
using System.Collections.Generic;
using System.IO;

namespace ModernFileCleaner.Services;

/// <summary>
/// Minimal .env loader (no external package).
/// Searches .env in: app base dir, current dir, then walks up toward the
/// project root so it works both in `dotnet run` and published builds.
/// </summary>
public static class AppEnv
{
    private static readonly Dictionary<string, string> _values = new(System.StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        var dirs = new List<string> { AppContext.BaseDirectory, Environment.CurrentDirectory };
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 4 && dir != null; i++)
        {
            dirs.Add(dir.FullName);
            dir = dir.Parent;
        }

        foreach (var d in dirs)
        {
            var path = Path.Combine(d, ".env");
            if (File.Exists(path)) { Parse(path); return; }
        }
    }

    private static void Parse(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim().Trim('"').Trim('\'');
            _values[key] = value;
        }
    }

    public static string Get(string key, string fallback = "")
    {
        EnsureLoaded();
        return _values.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded (AppEnv.cs wird noch nicht referenziert, kompiliert aber).

- [ ] **Step 6: Commit**

```bash
git add .env.example cleaner1/Services/AppEnv.cs .gitignore
git commit -m "chore: add .env config loader and KeyAuth env scaffolding"
```
Hinweis: `.env` nicht stagen (gitignore greift; prüfen mit `git status`).

---

### Task 2: `Ed25519.cs` vendoren

**Files:**
- Create: `cleaner1/Services/Ed25519.cs`

**Interfaces:**
- Consumes: — (wird von `KeyAuthService` in Task 4 genutzt)
- Produces: `Ed25519.CheckValid(byte[] signature, byte[] message, byte[] publicKey)` → `bool` (statisch, Namespace `ModernFileCleaner.Services`)

- [ ] **Step 1: Datei aus offizieller KeyAuth-SDK laden**

```bash
gh api repos/KeyAuth/KeyAuth-CSHARP-Example/contents/Console/Ed25519.cs -H "Accept: application/vnd.github.raw" > cleaner1/Services/Ed25519.cs
```
Public-Domain-Implementierung (Java→C#-Port von Hans Wolff, k3d3). 321 Zeilen.

- [ ] **Step 2: Namespace anpassen**

In `cleaner1/Services/Ed25519.cs` ändern: `namespace Cryptographic` → `namespace ModernFileCleaner.Services`.
Danach die Datei kurz prüfen: `public static bool CheckValid(byte[] signature, byte[] message, byte[] publicKey)` muss vorhanden sein (Zeile ~230).

- [ ] **Step 3: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add cleaner1/Services/Ed25519.cs
git commit -m "chore: vendor Ed25519 signature verification (public domain)"
```

---

### Task 3: `LicenseStorage` (DPAPI-Key-Ablage)

**Files:**
- Create: `cleaner1/Services/LicenseStorage.cs`

**Interfaces:**
- Consumes: —
- Produces: `LicenseStorage.Load()` → `string?`, `LicenseStorage.Save(string key)`, `LicenseStorage.Delete()` (alle statisch)

- [ ] **Step 1: `LicenseStorage.cs` schreiben**

```csharp
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
```

- [ ] **Step 2: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add cleaner1/Services/LicenseStorage.cs
git commit -m "refactor: add DPAPI license key storage"
```

---

### Task 4: `KeyAuthService` + Response-Modelle

**Files:**
- Create: `cleaner1/Services/KeyAuthService.cs`

**Interfaces:**
- Consumes: `AppEnv.Get` (Task 1), `Ed25519.CheckValid` (Task 2)
- Produces:
  - `KeyAuthResponse { bool Success; bool IsNetworkError; string? Message; string? SessionId; KeyAuthUserInfo? Info; }`
  - `KeyAuthUserInfo { string? Username; string? Ip; string? Hwid; List<KeyAuthSubscription> Subscriptions; }`
  - `KeyAuthSubscription { string? Subscription; string? Expiry; string? Timeleft; }`
  - `KeyAuthService` (IDisposable): `Task<KeyAuthResponse> InitAsync()`, `Task<KeyAuthResponse> LoginWithKeyAsync(string key)`, `Task<KeyAuthResponse> CheckAsync()`, Properties `SessionId`, `Username`, `Subscription`, `ExpiryUtc`, `IsAuthenticated`, statisch `GetHwid()` → `string`

- [ ] **Step 1: `KeyAuthService.cs` schreiben**

```csharp
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ModernFileCleaner.Services;

public class KeyAuthResponse
{
    public bool Success { get; set; }
    public bool IsNetworkError { get; set; }
    public string? Message { get; set; }
    public string? SessionId { get; set; }
    public KeyAuthUserInfo? Info { get; set; }
}

public class KeyAuthUserInfo
{
    public string? Username { get; set; }
    public string? Ip { get; set; }
    public string? Hwid { get; set; }
    public List<KeyAuthSubscription> Subscriptions { get; set; } = new();
}

public class KeyAuthSubscription
{
    public string? Subscription { get; set; }
    public string? Expiry { get; set; }
    public string? Timeleft { get; set; }
}

/// <summary>
/// KeyAuth API v1.3 client (https://keyauth.win/api/1.3/).
/// Session-based: InitAsync() first, then LoginWithKeyAsync(), then CheckAsync().
/// Every response is Ed25519-signed by KeyAuth; verification fails closed.
/// </summary>
public class KeyAuthService : IDisposable
{
    private const string ApiUrl = "https://keyauth.win/api/1.3/";

    // KeyAuth response-signing public key (from official C# SDK)
    private static readonly byte[] SignaturePublicKey =
        HexToBytes("5586b4bc69c7a4b487e4563a4cd96afd39140f919bd31cea7d1c6a1e8439422b");

    private readonly HttpClient _http;
    private readonly string _name;
    private readonly string _ownerid;
    private readonly string _version;

    public string? SessionId { get; private set; }
    public string? Username { get; private set; }
    public string? Subscription { get; private set; }
    public DateTime? ExpiryUtc { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public KeyAuthService()
    {
        _name = AppEnv.Get("KEYAUTH_NAME", "System sweep");
        _ownerid = AppEnv.Get("KEYAUTH_OWNERID");
        _version = AppEnv.Get("KEYAUTH_VERSION", "1.0");
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("SystemSweep/2.0");
    }

    public async Task<KeyAuthResponse> InitAsync()
    {
        var resp = await PostAsync(new Dictionary<string, string>
        {
            ["type"] = "init",
            ["ver"] = _version,
            ["name"] = _name,
            ["ownerid"] = _ownerid
        });
        if (resp.Success) SessionId = resp.SessionId;
        return resp;
    }

    public async Task<KeyAuthResponse> LoginWithKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return new KeyAuthResponse { Success = false, Message = "License key is empty" };

        var resp = await PostAsync(new Dictionary<string, string>
        {
            ["type"] = "license",
            ["key"] = key.Trim(),
            ["hwid"] = GetHwid(),
            ["sessionid"] = SessionId ?? "",
            ["name"] = _name,
            ["ownerid"] = _ownerid
        });

        if (resp.Success && resp.Info != null)
        {
            Username = resp.Info.Username;
            var sub = resp.Info.Subscriptions.FirstOrDefault();
            Subscription = sub?.Subscription;
            if (sub != null && long.TryParse(sub.Expiry, out var unix))
                ExpiryUtc = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            IsAuthenticated = true;
        }
        else if (resp.Success)
        {
            IsAuthenticated = true;
        }
        return resp;
    }

    public async Task<KeyAuthResponse> CheckAsync()
    {
        return await PostAsync(new Dictionary<string, string>
        {
            ["type"] = "check",
            ["sessionid"] = SessionId ?? "",
            ["name"] = _name,
            ["ownerid"] = _ownerid
        });
    }

    private async Task<KeyAuthResponse> PostAsync(Dictionary<string, string> data)
    {
        try
        {
            using var content = new FormUrlEncodedContent(data);
            using var response = await _http.PostAsync(ApiUrl, content);

            var rawBytes = await response.Content.ReadAsByteArrayAsync();
            string? sig = response.Headers.TryGetValues("x-signature-ed25519", out var sv) ? sv.FirstOrDefault() : null;
            string? ts = response.Headers.TryGetValues("x-signature-timestamp", out var tv) ? tv.FirstOrDefault() : null;

            if (string.IsNullOrEmpty(sig) || string.IsNullOrEmpty(ts) || !VerifySignature(sig, ts, rawBytes))
                return new KeyAuthResponse { Success = false, Message = "License server verification failed" };

            var body = Encoding.UTF8.GetString(rawBytes);
            var json = JObject.Parse(body);
            var result = new KeyAuthResponse
            {
                Success = json["success"]?.Value<bool>() ?? false,
                Message = json["message"]?.ToString(),
                SessionId = json["sessionid"]?.ToString()
            };

            var info = json["info"];
            if (info != null && info.Type != JTokenType.Null)
            {
                result.Info = new KeyAuthUserInfo
                {
                    Username = info["username"]?.ToString(),
                    Ip = info["ip"]?.ToString(),
                    Hwid = info["hwid"]?.ToString(),
                    Subscriptions = info["subscriptions"]?.ToObject<List<KeyAuthSubscription>>() ?? new List<KeyAuthSubscription>()
                };
            }
            return result;
        }
        catch (HttpRequestException ex)
        {
            return new KeyAuthResponse { Success = false, IsNetworkError = true, Message = $"Network error: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new KeyAuthResponse { Success = false, IsNetworkError = true, Message = "Connection timed out" };
        }
        catch (Exception ex)
        {
            return new KeyAuthResponse { Success = false, Message = ex.Message };
        }
    }

    private static bool VerifySignature(string signatureHex, string timestamp, byte[] rawResponse)
    {
        try
        {
            // Replay window: timestamp may be ~30s in the future, up to 60s old.
            if (!long.TryParse(timestamp, out var ts)) return false;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (ts - now > 30 || now - ts > 60) return false;

            var sig = HexToBytes(signatureHex);
            var tsBytes = Encoding.UTF8.GetBytes(timestamp);
            var body = new byte[tsBytes.Length + rawResponse.Length];
            Buffer.BlockCopy(tsBytes, 0, body, 0, tsBytes.Length);
            Buffer.BlockCopy(rawResponse, 0, body, tsBytes.Length, rawResponse.Length);
            return Ed25519.CheckValid(sig, body, SignaturePublicKey);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    /// <summary>Stable machine fingerprint (CPU + mainboard serial), used as KeyAuth HWID.</summary>
    public static string GetHwid()
    {
        try
        {
            var parts = new List<string>();
            using (var mc = new System.Management.ManagementClass("Win32_Processor"))
            foreach (var item in mc.GetInstances()) { parts.Add(item["ProcessorId"]?.ToString() ?? ""); break; }

            using (var mb = new System.Management.ManagementClass("Win32_BaseBoard"))
            foreach (var item in mb.GetInstances()) { parts.Add(item["SerialNumber"]?.ToString() ?? ""); break; }

            var raw = string.Join("-", parts.Where(p => !string.IsNullOrEmpty(p)));
            if (string.IsNullOrEmpty(raw)) raw = Environment.MachineName;
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        }
        catch
        {
            return "hwid-" + Environment.MachineName;
        }
    }

    public void Dispose() => _http.Dispose();
}
```

- [ ] **Step 2: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded. Falls Newtonsoft.Json-`ToObject` Warnung bzgl. fehlender Definition wirft: `using System.Collections.Generic;` oben ergänzen (bei ImplicitUsings normalerweise nicht nötig).

- [ ] **Step 3: Commit**

```bash
git add cleaner1/Services/KeyAuthService.cs
git commit -m "feat: add KeyAuthService with init/license/check and Ed25519 verification"
```

---

### Task 5: `AppLicense` Zustand

**Files:**
- Create: `cleaner1/AppLicense.cs`

**Interfaces:**
- Consumes: —
- Produces: `LicenseMode { Demo, Full }`, `AppLicense` statisch: `Mode`, `IsFullAccess` (`bool`), `Username`, `Subscription`, `ExpiryUtc` (`DateTime?`), `SetFull(string? username, string? subscription, DateTime? expiryUtc)`, `SetDemo()`

- [ ] **Step 1: `AppLicense.cs` schreiben**

```csharp
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
```

- [ ] **Step 2: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add cleaner1/AppLicense.cs
git commit -m "feat: add AppLicense state for full/demo mode"
```

---

### Task 6: Start-Flow in `App.xaml.cs` umbauen

**Files:**
- Modify: `cleaner1/App.xaml.cs` (OnStartup + alle Keyzy/HWID/License-Hilfsmethoden ersetzen)

**Interfaces:**
- Consumes: `AppEnv.EnsureLoaded`, `KeyAuthService`, `LicenseStorage`, `AppLicense`, `ActivationDialog` (parameterlos — unverändert aus Task 7), `RestoreDialog`
- Produces: `App.License` (`public static KeyAuthService`, eine Instanz für die App-Lebensdauer), `App.ValidateSavedKey()` (private static → bool), `App.PeriodicLicenseCheck()` (private static)

- [ ] **Step 1: `App.xaml.cs` ersatzweise neu schreiben**

`cleaner1/App.xaml.cs` komplett ersetzen durch:
```csharp
using System;
using System.Threading;
using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner
{
    public partial class App : Application
    {
        public static string[] StartupArgs = Array.Empty<string>();
        public static bool ProtectionPassed { get; private set; } = true;
        public static MainWindow? AppMainWindow { get; private set; }

        /// <summary>Shared KeyAuth session (one instance for the app lifetime).</summary>
        public static KeyAuthService License { get; private set; } = null!;

        private static Timer? _licenseTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartupArgs = e.Args;

            ProtectionPassed = ProtectionService.RunStartupChecks();
            if (!ProtectionPassed)
            {
                MessageBox.Show("Security checks failed. The application may be tampered with.\n\nPlease download a fresh copy from the official source.",
                    "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            AppSettings.Instance.Load();
            ThemeService.SetTheme(AppSettings.Instance.Theme);
            AppMainWindow = new MainWindow();

            // === License: KeyAuth (online only, session-based) ===
            AppEnv.EnsureLoaded();
            License = new KeyAuthService();

            if (!ValidateSavedKey())
            {
                var activationDialog = new ActivationDialog();
                bool? actResult = activationDialog.ShowDialog();
                if (actResult != true || !AppLicense.IsFullAccess)
                {
                    Current.Shutdown();
                    return;
                }
            }

            // Full access: periodic re-validation + restore-point dialog
            _licenseTimer = new Timer(
                async _ => await PeriodicLicenseCheck(),
                null,
                TimeSpan.FromMinutes(4),
                TimeSpan.FromMinutes(4));

            var restoreDialog = new RestoreDialog();
            restoreDialog.ShowDialog();

            AppMainWindow.Show();
            AppMainWindow.Activate();
            AppMainWindow.Focus();
        }

        /// <summary>
        /// Re-authenticates a saved key against KeyAuth. Returns true on success.
        /// On any failure (invalid/expired/offline) the stored key is deleted.
        /// </summary>
        private static bool ValidateSavedKey()
        {
            string? savedKey = LicenseStorage.Load();
            if (string.IsNullOrEmpty(savedKey)) return false;

            bool ok = Task.Run(async () =>
            {
                var init = await License.InitAsync();
                if (!init.Success) return false;
                var login = await License.LoginWithKeyAsync(savedKey!);
                return login.Success;
            }).GetAwaiter().GetResult();

            if (ok)
            {
                AppLicense.SetFull(License.Username, License.Subscription, License.ExpiryUtc);
                return true;
            }

            LicenseStorage.Delete();
            return false;
        }

        /// <summary>
        /// Periodically re-validates the KeyAuth session. Network errors are retried
        /// on the next interval; a hard rejection shuts the app down.
        /// </summary>
        private static async Task PeriodicLicenseCheck()
        {
            try
            {
                var check = await License.CheckAsync();
                if (check.Success || check.IsNetworkError) return;

                _licenseTimer?.Dispose();
                await AppMainWindow!.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("Your license has become invalid or expired.\n\nThe application will now close.",
                        "License Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Current.Shutdown();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[License] Periodic check error: {ex.Message}");
            }
        }
    }
}
```

Hinweis: `Task` stammt aus `System.Threading.Tasks` (implicit using). Die alten Methoden `VerifyLicenseWithKeyzy`, `VerifyHwidMatch`, `LoadLicenseKey`, `GetCurrentHwid` und das Feld `_savedLicenseKey` entfallen.

- [ ] **Step 2: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded. (ActivationDialog hat noch den parameterlosen Konstruktor — Task 7 ersetzt seinen Inhalt.)

- [ ] **Step 3: Commit**

```bash
git add cleaner1/App.xaml.cs
git commit -m "refactor: rewrite startup license flow for KeyAuth"
```

---

### Task 7: `ActivationDialog` auf KeyAuth + Demo-Modus

**Files:**
- Modify: `cleaner1/ActivationDialog.xaml`
- Modify: `cleaner1/ActivationDialog.xaml.cs`

**Interfaces:**
- Consumes: `App.License` (Task 6), `AppLicense` (Task 5), `LicenseStorage` (Task 3)
- Produces: `ActivationDialog` (parameterlos), Properties `IsActivated` (`bool`), `IsDemo` (`bool`)

- [ ] **Step 1: XAML anpassen**

In `cleaner1/ActivationDialog.xaml`:
- Zeile 16 Untertitel: `Enter your license key to unlock all features. You can also start a free trial.` → `Enter your license key to unlock all features, or continue in demo mode.`
- Zeile 30 Hinweis: `Enter your license key (RSA offline or Keyzy online)` → `Enter your KeyAuth license key`
- Zeile 37-39: `btnTrial` → `btnDemo`:
```xml
<ui:Button x:Name="btnDemo" Content="🔒  Use Demo Mode" Appearance="Secondary"
           Width="220" Height="34" HorizontalAlignment="Center" Margin="0,0,0,4"
           Click="Demo_Click"/>
```
- Zeile 41: `Trial: 30 days full access` → `Demo: browse only — cleaning and tweaks are disabled`

- [ ] **Step 2: Code-Behind neu schreiben**

`cleaner1/ActivationDialog.xaml.cs` komplett ersetzen:
```csharp
using System.Windows;
using System.Windows.Media;
using ModernFileCleaner.Services;

namespace ModernFileCleaner;

public partial class ActivationDialog : Window
{
    public bool IsActivated { get; private set; }
    public bool IsDemo { get; private set; }
    private DateTime _lastAttempt = DateTime.MinValue;
    private int _attemptCount = 0;
    private const int MaxAttempts = 5;

    public ActivationDialog()
    {
        InitializeComponent();
    }

    private async void Activate_Click(object sender, RoutedEventArgs e)
    {
        _attemptCount++;
        if (_attemptCount > MaxAttempts)
        {
            txtStatus.Text = "❌ Too many attempts. Restart the app to try again.";
            btnActivate.IsEnabled = false;
            return;
        }
        var elapsed = DateTime.Now - _lastAttempt;
        if (elapsed.TotalSeconds < 2)
        {
            txtStatus.Text = "⏳ Please wait...";
            await Task.Delay(2000 - (int)elapsed.TotalMilliseconds);
        }
        _lastAttempt = DateTime.Now;

        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            txtStatus.Text = "⚠️ Please enter a license key";
            return;
        }

        btnActivate.IsEnabled = false;
        txtStatus.Text = "🔍 Validating license...";
        StatusBox.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x00, 0x78, 0xD4));

        var init = await App.License.InitAsync();
        if (!init.Success)
        {
            ShowError(init.Message ?? "Could not connect to license server");
            return;
        }

        var login = await App.License.LoginWithKeyAsync(key);
        if (!login.Success)
        {
            ShowError(login.Message ?? "Invalid license key");
            return;
        }

        AppLicense.SetFull(App.License.Username, App.License.Subscription, App.License.ExpiryUtc);
        LicenseStorage.Save(key);
        txtStatus.Text = $"✅ Activated! Welcome, {App.License.Username ?? "User"}!";
        StatusBox.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x4C, 0xAF, 0x50));
        IsActivated = true;
        await Task.Delay(800);
        DialogResult = true;
        Close();
    }

    private void Demo_Click(object sender, RoutedEventArgs e)
    {
        AppLicense.SetDemo();
        IsDemo = true;
        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        txtStatus.Text = $"❌ {message}";
        StatusBox.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0xF4, 0x43, 0x36));
        btnActivate.IsEnabled = true;
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded. Keine Verweise mehr auf `LicenseService`/`KeyzyLicenseService` in dieser Datei.

- [ ] **Step 4: Commit**

```bash
git add cleaner1/ActivationDialog.xaml cleaner1/ActivationDialog.xaml.cs
git commit -m "feat: rework ActivationDialog for KeyAuth + demo mode"
```

---

### Task 8: `LicensePage` auf KeyAuth-Felder

**Files:**
- Modify: `cleaner1/Pages/LicensePage.xaml`
- Modify: `cleaner1/Pages/LicensePage.xaml.cs`

**Interfaces:**
- Consumes: `AppLicense` (Task 5), `KeyAuthService.GetHwid` (Task 4), `App.License` (Task 6), `LicenseStorage` (Task 3)
- Produces: `LicensePage` (parameterlos, wird von `MainWindow.NavigateTo` erzeugt)

- [ ] **Step 1: XAML anpassen**

In `cleaner1/Pages/LicensePage.xaml`:
- Zeile 65: Label `Trial Days Left` → `Subscription`
- Zeile 66: `x:Name="txtTrialDays"` → `x:Name="txtSubscription"`
- Zeile 88: `Enter your Keyzy.io license key` → `Enter your KeyAuth license key`
- Zeile 89: `Click="ActivateKeyzy_Click"` → `Click="ActivateKeyAuth_Click"`
- Zeile 104: `Click="ContinueTrial_Click"` → `Click="Continue_Click"`

- [ ] **Step 2: Code-Behind neu schreiben**

`cleaner1/Pages/LicensePage.xaml.cs` komplett ersetzen:
```csharp
using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner.Pages;

public partial class LicensePage
{
    public LicensePage()
    {
        InitializeComponent();
        LoadLicenseInfo();
    }

    private void LoadLicenseInfo()
    {
        txtHwid.Text = KeyAuthService.GetHwid();

        if (AppLicense.IsFullAccess)
            ShowActivated();
        else if (AppLicense.Mode == LicenseMode.Demo)
            ShowDemo();
        else
            ShowNoLicense();
    }

    private void ShowActivated()
    {
        txtIcon.Text = "✅";
        txtTitle.Text = "License Active";
        txtSubtitle.Text = "All features unlocked";

        StatusCard.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1A, 0x4C, 0xAF, 0x50));
        StatusCard.BorderBrush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x33, 0x4C, 0xAF, 0x50));
        txtLicenseStatus.Text = "✅ Fully Activated";
        txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(76, 175, 80));
        txtLicenseDetail.Text = "License is valid via KeyAuth";

        txtLicStatus.Text = "✅ Active";
        txtLicStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(76, 175, 80));

        var savedKey = LicenseStorage.Load() ?? "";
        txtLicKey.Text = savedKey.Length > 24 ? savedKey[..8] + "..." + savedKey[^8..] : (savedKey.Length > 0 ? savedKey : "—");
        txtLicUser.Text = AppLicense.Username ?? "Licensed User";
        txtLicExpiry.Text = AppLicense.ExpiryUtc.HasValue
            ? AppLicense.ExpiryUtc.Value.ToLocalTime().ToString("yyyy-MM-dd")
            : "Perpetual";
        txtLicExpiry.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(76, 175, 80));
        txtSubscription.Text = AppLicense.Subscription ?? "Default";

        ActivationCard.Visibility = Visibility.Collapsed;
        btnContinue.Content = "⏭  Go to Dashboard";
    }

    private void ShowDemo()
    {
        txtIcon.Text = "🔒";
        txtTitle.Text = "Demo Mode";
        txtSubtitle.Text = "Browse only — actions are disabled";

        StatusCard.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1A, 0xFF, 0xAA, 0x00));
        StatusCard.BorderBrush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0xAA, 0x00));
        txtLicenseStatus.Text = "🔒 Demo Mode";
        txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(255, 193, 7));
        txtLicenseDetail.Text = "Enter a license key to unlock all features";

        txtLicStatus.Text = "🔒 Demo";
        txtLicStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(255, 193, 7));
        txtLicKey.Text = "—";
        txtLicUser.Text = Environment.UserName;
        txtLicExpiry.Text = "—";
        txtSubscription.Text = "—";

        ActivationCard.Visibility = Visibility.Visible;
        btnContinue.Content = "⏭  Continue in Demo";
    }

    private void ShowNoLicense()
    {
        txtIcon.Text = "🔐";
        txtTitle.Text = "Activate System Sweep";
        txtSubtitle.Text = "Enter your license key below";

        StatusCard.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x1A, 0x00, 0x78, 0xD4));
        txtLicenseStatus.Text = "Not Activated";
        txtLicenseStatus.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4));
        txtLicenseDetail.Text = "Enter a license key to unlock all features";

        txtLicStatus.Text = "⏳ Not Activated";
        txtLicUser.Text = Environment.UserName;
        txtLicExpiry.Text = "—";
        txtSubscription.Text = "—";

        ActivationCard.Visibility = Visibility.Visible;
        btnContinue.Content = "⏭  Start Demo";
    }

    private async void ActivateKeyAuth_Click(object sender, RoutedEventArgs e)
    {
        var key = txtLicenseKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            txtLicenseDetail.Text = "Please enter a license key";
            return;
        }

        btnActivate.IsEnabled = false;
        txtLicenseStatus.Text = "🔍 Validating...";

        var init = await App.License.InitAsync();
        if (!init.Success)
        {
            txtLicenseDetail.Text = $"❌ {init.Message}";
            btnActivate.IsEnabled = true;
            return;
        }

        var login = await App.License.LoginWithKeyAsync(key);
        if (login.Success)
        {
            AppLicense.SetFull(App.License.Username, App.License.Subscription, App.License.ExpiryUtc);
            LicenseStorage.Save(key);
            MessageBox.Show($"✅ License activated successfully!\n\nWelcome, {App.License.Username ?? "User"}!",
                "Activation Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadLicenseInfo();
        }
        else
        {
            txtLicenseDetail.Text = $"❌ {login.Message}";
            btnActivate.IsEnabled = true;
        }
    }

    private void BuyLicense_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://keyauth.cc/app/") { UseShellExecute = true });
        }
        catch { }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)Application.Current.MainWindow;
        mainWindow.NavigateToCleanPage();
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded. Keine Verweise mehr auf `LicenseService`/`KeyzyLicenseService` in dieser Datei.

- [ ] **Step 4: Commit**

```bash
git add cleaner1/Pages/LicensePage.xaml cleaner1/Pages/LicensePage.xaml.cs
git commit -m "feat: update LicensePage for KeyAuth fields"
```

---

### Task 9: Demo-Modus — Aktionen sperren

**Files:**
- Modify: `cleaner1/Pages/CleanPage.xaml.cs`
- Modify: `cleaner1/Pages/DashboardPage.xaml.cs`
- Modify: `cleaner1/Pages/TweaksPage.xaml.cs`
- Modify: `cleaner1/Pages/BrowserCachePage.xaml.cs`
- Modify: `cleaner1/Pages/DuplicatesPage.xaml.cs`
- Modify: `cleaner1/Pages/StartupPage.xaml.cs`

**Interfaces:**
- Consumes: `AppLicense.IsFullAccess` (Task 5)

- [ ] **Step 1: CleanPage — Buttons sperren + Guards**

`cleaner1/Pages/CleanPage.xaml.cs`:
- Im Konstruktor nach `InitializeComponent();`:
```csharp
if (!AppLicense.IsFullAccess) { btnAnalyze.IsEnabled = false; btnClean.IsEnabled = false; }
```
- Am Anfang von `btnAnalyze_Click` und `btnClean_Click` je:
```csharp
if (!AppLicense.IsFullAccess) { txtStatus.Text = "🔒 Demo — license key required"; return; }
```

- [ ] **Step 2: DashboardPage — Guards**

`cleaner1/Pages/DashboardPage.xaml.cs`:
- Am Anfang von `QuickClean_Click` und `EmptyRecycle_Click` je:
```csharp
if (!AppLicense.IsFullAccess) return;
```

- [ ] **Step 3: TweaksPage — Toggles + Guards**

`cleaner1/Pages/TweaksPage.xaml.cs`:
- In `CreateTweakCard` nach der Toggle-Erstellung:
```csharp
toggle.IsEnabled = AppLicense.IsFullAccess;
```
- Am Anfang von `TweakToggle_Click`, `ApplyRecommended_Click`, `RevertAll_Click` je:
```csharp
if (!AppLicense.IsFullAccess) { txtStatus.Text = "🔒 Demo — license key required"; return; }
```

- [ ] **Step 4: BrowserCachePage — Clean-Button + Guard**

`cleaner1/Pages/BrowserCachePage.xaml.cs`:
- Im Konstruktor:
```csharp
btnClean.IsEnabled = AppLicense.IsFullAccess;
```
- Am Anfang von `Clean_Click`:
```csharp
if (!AppLicense.IsFullAccess) { txtStatus.Text = "🔒 Demo — license key required"; return; }
```
- In `Scan_Click` (nach Abschluss): `btnClean.IsEnabled = true;` → `btnClean.IsEnabled = AppLicense.IsFullAccess;`

- [ ] **Step 5: DuplicatesPage — Delete-Button + Guard**

`cleaner1/Pages/DuplicatesPage.xaml.cs`:
- Im Konstruktor nach `cmbPath.SelectedIndex = 0;`:
```csharp
btnDelete.IsEnabled = AppLicense.IsFullAccess;
```
- Am Anfang von `btnDelete_Click`:
```csharp
if (!AppLicense.IsFullAccess) { MessageBox.Show("🔒 Demo — license key required", "Demo Mode", MessageBoxButton.OK, MessageBoxImage.Information); return; }
```
- In `btnScan_Click`: beide Vorkommen von `btnDelete.IsEnabled = ...` → `btnDelete.IsEnabled = AppLicense.IsFullAccess && (...)`:
  - `btnDelete.IsEnabled = _duplicates.Count > 0;` → `btnDelete.IsEnabled = AppLicense.IsFullAccess && _duplicates.Count > 0;`
  - (im `finally` nichts, im Methodenende) `btnDelete.IsEnabled = _duplicates.Count > 0;` → `btnDelete.IsEnabled = AppLicense.IsFullAccess && _duplicates.Count > 0;`

- [ ] **Step 6: StartupPage — Guard mit Revert**

`cleaner1/Pages/StartupPage.xaml.cs`:
- `ToggleSwitch_Click` komplett ersetzen:
```csharp
private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
{
    if (!AppLicense.IsFullAccess)
    {
        if (sender is Wpf.Ui.Controls.ToggleSwitch demo) demo.IsChecked = !(demo.IsChecked ?? false);
        MessageBox.Show("🔒 Demo — license key required", "Demo Mode",
            MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }

    if (sender is Wpf.Ui.Controls.ToggleSwitch toggle && toggle.DataContext is StartupItem item)
    {
        _startupService.Toggle(item);
    }
}
```

- [ ] **Step 7: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded.

- [ ] **Step 8: Commit**

```bash
git add cleaner1/Pages/CleanPage.xaml.cs cleaner1/Pages/DashboardPage.xaml.cs cleaner1/Pages/TweaksPage.xaml.cs cleaner1/Pages/BrowserCachePage.xaml.cs cleaner1/Pages/DuplicatesPage.xaml.cs cleaner1/Pages/StartupPage.xaml.cs
git commit -m "feat: gate actions behind full license in demo mode"
```

---

### Task 10: `AboutPage` — Lizenzstatus-Zeile

**Files:**
- Modify: `cleaner1/Pages/AboutPage.xaml`
- Modify: `cleaner1/Pages/AboutPage.xaml.cs`

**Interfaces:**
- Consumes: `AppLicense` (Task 5)

- [ ] **Step 1: XAML — License-Zeile benennen**

In `cleaner1/Pages/AboutPage.xaml` Zeile 42:
`<TextBlock Grid.Column="1" Text="Single-User License" Foreground="White" FontSize="13"/>`
→
```xml
<TextBlock Grid.Column="1" x:Name="txtLicenseInfo" Text="—" Foreground="White" FontSize="13"/>
```

- [ ] **Step 2: Code-Behind — Status setzen**

In `cleaner1/Pages/AboutPage.xaml.cs` im Konstruktor nach `txtVersion.Text = ...`:
```csharp
txtLicenseInfo.Text = AppLicense.IsFullAccess
    ? $"KeyAuth · {AppLicense.Username ?? "User"}"
    : "Demo Mode";
```

- [ ] **Step 3: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add cleaner1/Pages/AboutPage.xaml cleaner1/Pages/AboutPage.xaml.cs
git commit -m "feat: show license status on About page"
```

---

### Task 11: Alt-Lizenzsystem entfernen

**Files:**
- Delete: `cleaner1/Services/LicenseService.cs`
- Delete: `cleaner1/Services/KeyzyLicenseService.cs`
- Delete: `cleaner1/keyzy-config.example.json`
- Delete: `tools/KeyGenerator/` (kompletter Ordner inkl. `private.key`, `public-key-cs.txt`, `Program.cs`, `KeyGenerator.csproj`, `bin/`, `obj/`)

**Interfaces:**
- Consumes: — (Tasks 6–8 haben alle Referenzen auf die alten Klassen entfernt)

- [ ] **Step 1: Referenzen prüfen (vor Löschung)**

Run:
```bash
git grep -n "LicenseService\|KeyzyLicenseService\|keyzy-config\|KeyGenerator" -- 'cleaner1/*.cs' 'cleaner1/**/*.cs' '*.csproj' '*.sln' 2>/dev/null
```
Expected: Nur noch Treffer in `obj/`-Buildartefakten (die von git ignoriert werden) oder `keyzy-config.json` (gitignored). Keine Quellcode-Treffer in `cleaner1/` (außer `.gitignore`-Zeile). Treffer in `docs/`, `.remember/` o. ä. sind egal.

- [ ] **Step 2: Dateien löschen**

```bash
git rm cleaner1/Services/LicenseService.cs cleaner1/Services/KeyzyLicenseService.cs cleaner1/keyzy-config.example.json
git rm -r tools/KeyGenerator
```

- [ ] **Step 3: Build**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded. (`tools/KeyGenerator` ist keine Referenz aus `cleaner1.sln`-Build nötig — Solution prüfen, falls Build meldet: `git rm -r` hat nur das Tool entfernt, das separat gebaut wurde.)

- [ ] **Step 4: Commit**

```bash
git commit -m "refactor: remove RSA/Keyzy licensing and KeyGenerator tool"
```

---

### Task 12: README dokumentieren + finale Verifikation

**Files:**
- Modify: `README.md`

**Interfaces:**
- Consumes: —

- [ ] **Step 1: README-Abschnitt ergänzen**

Am Ende von `README.md` (neuer Abschnitt, deutsch/englisch konsistent zum Rest — englisch wie der Rest):
```markdown
## License Configuration (KeyAuth)

Licensing is handled by KeyAuth (https://keyauth.cc). The app authenticates a
license key against the KeyAuth API on every start (online only) and re-checks
the session every 4 minutes. There is no offline mode.

Configuration is read at runtime from a `.env` file placed next to the exe or
in the project root:

| Variable | Description |
|---|---|
| `KEYAUTH_NAME` | KeyAuth application name |
| `KEYAUTH_OWNERID` | KeyAuth owner id |
| `KEYAUTH_SECRET` | KeyAuth application secret (used by Seller API; not sent by the v1.3 client) |
| `KEYAUTH_VERSION` | App version as configured in the KeyAuth dashboard |

Copy `.env.example` to `.env`, fill in the values from your KeyAuth dashboard,
and never commit `.env`. Users who enter a valid key get full access; the
"Demo Mode" button on the activation screen runs the app with all cleaning and
tweak actions disabled.
```

- [ ] **Step 2: `dotnet build`**

Run: `dotnet build cleaner1/cleaner1.csproj --nologo`
Expected: Build succeeded.

- [ ] **Step 3: Endgültige Verifikation — keine Alt-Verweise**

```bash
git grep -n -i "keyzy" -- '*.cs' '*.xaml' '*.csproj' | grep -v obj/ || echo "no keyzy refs"
git grep -n "LicenseService\b" -- 'cleaner1/*.cs' 'cleaner1/**/*.cs' 2>/dev/null | grep -v obj/ || echo "no LicenseService refs"
```
Expected: keine Treffer (oder nur obj/).

Zusätzlich: `.env` ist nicht in git (`git status` zeigt `.env` nicht an), `.env.example` ist committet.

- [ ] **Step 4: Commit**

```bash
git add README.md
git commit -m "docs: document KeyAuth license configuration"
```

- [ ] **Step 5: Zusammenfassung an Nutzer**

Liste aller geänderten/entfernten/neu angelegten Dateien (siehe Abschnitt unten) und Hinweis auf `.env`-Befüllung + KeyAuth-Dashboard-Version.

---

## Erwartete Datei-Bilanz

**Neu:**
- `cleaner1/Services/AppEnv.cs`
- `cleaner1/Services/Ed25519.cs`
- `cleaner1/Services/LicenseStorage.cs`
- `cleaner1/Services/KeyAuthService.cs`
- `cleaner1/AppLicense.cs`
- `.env` (gitignored)
- `.env.example`

**Geändert:**
- `.gitignore`
- `cleaner1/App.xaml.cs`
- `cleaner1/ActivationDialog.xaml`, `cleaner1/ActivationDialog.xaml.cs`
- `cleaner1/Pages/LicensePage.xaml`, `.xaml.cs`
- `cleaner1/Pages/CleanPage.xaml.cs`
- `cleaner1/Pages/DashboardPage.xaml.cs`
- `cleaner1/Pages/TweaksPage.xaml.cs`
- `cleaner1/Pages/BrowserCachePage.xaml.cs`
- `cleaner1/Pages/DuplicatesPage.xaml.cs`
- `cleaner1/Pages/StartupPage.xaml.cs`
- `cleaner1/Pages/AboutPage.xaml`, `.xaml.cs`
- `README.md`

**Entfernt:**
- `cleaner1/Services/LicenseService.cs` (RSA)
- `cleaner1/Services/KeyzyLicenseService.cs`
- `cleaner1/keyzy-config.example.json`
- `tools/KeyGenerator/` (inkl. committeter `private.key`)
