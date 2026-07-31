namespace ModernFileCleaner.Models;

/// <summary>An app that can be silently installed via winget (Ninite-style installer).</summary>
public class InstallableApp
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Category { get; set; } = "Utilities";
    public bool IsSelected { get; set; }
}
