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
