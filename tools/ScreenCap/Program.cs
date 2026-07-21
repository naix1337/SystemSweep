using System.Drawing.Imaging;
using System.Runtime.InteropServices;
const int MOUSEEVENTF_LEFTDOWN = 0x0002;
const int MOUSEEVENTF_LEFTUP = 0x0004;

string outputDir = args.Length > 0 ? args[0] : ".";

// Pages to capture: each has a Y-coordinate offset to click in the sidebar
var pages = new (string Name, int ClickY)[]
{
    ("dashboard", 200),  // 1st item in sidebar
    ("clean", 250),      // 2nd item
    ("browsers", 300),   // 3rd item
    ("duplicates", 350), // 4th item
    ("startup", 400),    // 5th item
    ("tweaks", 450),     // 6th item
    ("stats", 530),      // 7th item (after "TOOLS" label)
    ("settings", 580),   // 8th item
    ("about", 630),      // 9th item
};

IntPtr hWnd = FindWindow(null, "System Sweep");
if (hWnd == IntPtr.Zero) { Console.Error.WriteLine("Window not found!"); return 1; }

RECT rect;
GetWindowRect(hWnd, out rect);
int winX = rect.Left, winY = rect.Top;

// Capture current (dashboard)
SetForegroundWindow(hWnd);
Thread.Sleep(800);
Capture(winX, winY, rect.Right - rect.Left, rect.Bottom - rect.Top, Path.Combine(outputDir, "dashboard-screenshot.png"));

// For each other page, click the nav item and capture
foreach (var page in pages.Skip(1))
{
    // Click sidebar item (left side: x=110, y = winY + page.ClickY)
    int clickX = winX + 110;
    int clickY = winY + page.ClickY;

    // Move mouse and click
    SetCursorPos(clickX, clickY);
    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
    Thread.Sleep(50);
    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    Thread.Sleep(800);

    Capture(winX, winY, rect.Right - rect.Left, rect.Bottom - rect.Top,
        Path.Combine(outputDir, $"{page.Name}-screenshot.png"));
    Console.WriteLine($"  Captured: {page.Name}");
}

Console.WriteLine($"Done! {pages.Length} screenshots saved to {outputDir}");
return 0;

static void Capture(int x, int y, int w, int h, string path)
{
    using var bmp = new Bitmap(w, h);
    using var g = Graphics.FromImage(bmp);
    g.CopyFromScreen(x, y, 0, 0, new Size(w, h));
    bmp.Save(path, ImageFormat.Png);
}

[DllImport("user32.dll")] static extern IntPtr FindWindow(string cls, string win);
[DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
[DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
[DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
