# System Sweep Screenshot Capture Tool
# Run this while System Sweep is open on each page
# Saves screenshots to docs/assets/

param(
    [string]$OutputDir = "./docs/assets"
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public class ScreenCapture {
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
    public struct RECT { public int Left, Top, Right, Bottom; }
    public static Bitmap CaptureWindow(string title) {
        IntPtr hWnd = FindWindow(null, title);
        if (hWnd == IntPtr.Zero) return null;
        GetWindowRect(hWnd, out RECT rect);
        int w = rect.Right - rect.Left, h = rect.Bottom - rect.Top;
        if (w <= 0 || h <= 0) return null;
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(w, h));
        return bmp;
    }
}
"@

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "=== System Sweep Screenshot Capture ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Open System Sweep and navigate to each page, then press a key to capture."
Write-Host "Press Ctrl+C to exit."
Write-Host ""

# Capture each page
$pages = @(
    @{Name = "dashboard-screenshot"; Title = "System Sweep"},
    @{Name = "cleaning-screenshot"; Title = "System Sweep"},
    @{Name = "tweaks-screenshot"; Title = "System Sweep"},
    @{Name = "browser-cache-screenshot"; Title = "System Sweep"},
    @{Name = "duplicates-screenshot"; Title = "System Sweep"},
    @{Name = "startup-screenshot"; Title = "System Sweep"},
    @{Name = "statistics-screenshot"; Title = "System Sweep"},
    @{Name = "settings-screenshot"; Title = "System Sweep"},
    @{Name = "about-screenshot"; Title = "System Sweep"}
)

foreach ($page in $pages) {
    Write-Host "Navigate to $($page.Name) and press Enter to capture..." -ForegroundColor Yellow
    Read-Host

    $bmp = [ScreenCapture]::CaptureWindow($page.Title)
    if ($bmp) {
        $path = Join-Path $OutputDir "$($page.Name).png"
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "  ✅ Saved: $path" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Could not find window: $($page.Title)" -ForegroundColor Red
        Write-Host "  Make sure System Sweep is running!" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== All screenshots captured! ===" -ForegroundColor Cyan
Write-Host "Files saved to: $OutputDir" -ForegroundColor Cyan
