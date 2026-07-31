using Microsoft.Win32;

namespace ModernFileCleaner.Services;

/// <summary>
/// Captures the value of every registry key a tweak is about to write BEFORE it is
/// written (session-scoped). Reverting a tweak then restores the exact prior value
/// instead of a hardcoded default. Registry keys written by PowerShell commands
/// (services, powercfg, bcdedit, ...) are not captured and keep their hardcoded revert.
/// </summary>
public static class TweakSnapshotService
{
    private sealed class Snap
    {
        public object? Value;
        public RegistryValueKind Kind;
        public bool Existed;
    }

    private static readonly Dictionary<string, Snap> _snapshots = new();
    private static readonly Dictionary<string, List<string>> _tweakKeys = new();
    private static string _currentTweakId = "";
    private static readonly List<string> _currentKeys = new();

    public static void BeginTweak(string tweakId)
    {
        _currentTweakId = tweakId;
        _currentKeys.Clear();
    }

    public static void EndTweak()
    {
        if (_currentTweakId.Length > 0 && _currentKeys.Count > 0)
            _tweakKeys[_currentTweakId] = new List<string>(_currentKeys);
        _currentTweakId = "";
        _currentKeys.Clear();
    }

    /// <summary>Record the value that exists now (before the tweak writes it). Idempotent per key.</summary>
    public static void Snapshot(bool localMachine, string path, string name)
    {
        var key = KeyOf(localMachine, path, name);
        if (!_snapshots.ContainsKey(key))
        {
            var snap = new Snap { Existed = false };
            try
            {
                var hive = localMachine ? Registry.LocalMachine : Registry.CurrentUser;
                using var rk = hive.OpenSubKey(path);
                if (rk != null)
                {
                    var v = rk.GetValue(name);
                    if (v != null) { snap.Value = v; snap.Kind = rk.GetValueKind(name); snap.Existed = true; }
                }
            }
            catch { }
            _snapshots[key] = snap;
        }
        if (_currentTweakId.Length > 0 && !_currentKeys.Contains(key))
            _currentKeys.Add(key);
    }

    /// <summary>
    /// Restore the exact prior value for every key this tweak wrote during its apply.
    /// Called at the end of RevertTweakAsync so exact values win over the hardcoded
    /// defaults; tweaks not applied this session are a no-op (hardcoded revert stays).
    /// </summary>
    public static void RestoreTweak(string tweakId)
    {
        if (!_tweakKeys.TryGetValue(tweakId, out var keys)) return;
        foreach (var k in keys) RestoreFromKey(k, null);
    }

    private static void RestoreFromKey(string key, object? fallback)
    {
        // key format: LM\path\name or CU\path\name
        try
        {
            var local = key.StartsWith("LM\\");
            var rest = key[3..];
            var idx = rest.LastIndexOf('\\');
            if (idx <= 0) return;
            var path = rest[..idx];
            var name = rest[(idx + 1)..];
            var hive = local ? Registry.LocalMachine : Registry.CurrentUser;
            using var rk = hive.OpenSubKey(path, true);
            if (_snapshots.TryGetValue(key, out var snap))
            {
                if (snap.Existed)
                {
                    using var writeKey = rk ?? hive.CreateSubKey(path);
                    if (snap.Value != null) writeKey?.SetValue(name, snap.Value, snap.Kind);
                }
                else
                {
                    rk?.DeleteValue(name, false);
                }
                return;
            }
            if (fallback != null)
            {
                using var writeKey = rk ?? hive.CreateSubKey(path);
                writeKey?.SetValue(name, fallback);
            }
        }
        catch { }
    }

    private static string KeyOf(bool localMachine, string path, string name) => $"{(localMachine ? "LM" : "CU")}\\{path}\\{name}";
}
