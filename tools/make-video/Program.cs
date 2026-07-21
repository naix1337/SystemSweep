using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;

string assetsDir = args.Length > 0 ? args[0] : "../../docs/assets";
string outputDir = "../../docs/assets/video-frames";

Directory.CreateDirectory(outputDir);

// Font
PrivateFontCollection fonts = new();
try { fonts.AddFontFile("C:/Windows/Fonts/segui.ttf"); } catch { }
try { fonts.AddFontFile("C:/Windows/Fonts/segoeui.ttf"); } catch { }
try { fonts.AddFontFile("C:/Windows/Fonts/arial.ttf"); } catch { }
FontFamily titleFontFamily = fonts.Families.Length > 0 ? fonts.Families[0] : new FontFamily("Arial");
FontFamily fontFamily = fonts.Families.Length > 1 ? fonts.Families[1] : titleFontFamily;

// ========== Title Card ==========
CreateCard("System Sweep Professional", "The Ultimate Windows Optimization Toolkit", "⬇ Download at GitHub",
    "🎮 Gaming • ⚡ Performance • 🧹 Cleaning • 🗂️ Duplicates",
    "v2.0", Path.Combine(outputDir, "title.png"), 5);

// ========== Feature Cards ==========
var features = new (string Title, string Desc, string Icon, string Details, string Screenshot)[]
{
    ("Live Dashboard",     "Real-time system monitoring", "📊", "CPU • RAM • Disk • Uptime • Health Score",     "dashboard-screenshot.png"),
    ("System Cleaning",    "10 categories with safety levels", "🧹", "Temp • Cache • Logs • Windows.old",         "clean-screenshot.png"),
    ("Performance Tweaks", "30+ proven optimizations", "⚡", "Gaming FPS • System • Network • Disk • Memory",    "tweaks-screenshot.png"),
    ("Browser Cache",      "Clear Chrome, Edge, Firefox, Brave", "🌐", "Scan • Clean • Reclaim GBs",              "browsers-screenshot.png"),
    ("Duplicate Finder",   "SHA256-based duplicate detection", "🗂️", "Find • Preview • Delete with confidence",    "duplicates-screenshot.png"),
    ("Startup Manager",    "Control autostart programs", "🚀", "Registry • Startup Folder • One-click toggle",   "startup-screenshot.png"),
    ("Statistics",         "Cleaning history & reports", "📈", "Sessions • Space freed • HTML report export",    "stats-screenshot.png"),
};

int idx = 1;
int totalFrames = features.Length + 2; // title + features + outro

// Title
Console.WriteLine($"[1/{totalFrames}] Title card");

// Feature cards with screenshots
foreach (var f in features)
{
    idx++;
    string framePath = Path.Combine(outputDir, $"frame-{idx:D2}.png");
    string bgPath = Path.Combine(assetsDir, f.Screenshot);

    using var bg = File.Exists(bgPath) ? new Bitmap(bgPath) : new Bitmap(1280, 720);
    using var frame = new Bitmap(1280, 720);
    using var g = Graphics.FromImage(frame);
    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

    // Background (screenshot scaled)
    g.DrawImage(bg, 0, 0, 1280, 720);

    // Dark overlay
    using var overlay = new SolidBrush(Color.FromArgb(180, 13, 13, 13));
    g.FillRectangle(overlay, 0, 0, 1280, 720);

    // Icon
    using var iconFont = new Font(fontFamily, 64, FontStyle.Regular);
    using var iconBrush = new SolidBrush(Color.White);
    g.DrawString(f.Icon, iconFont, iconBrush, 640, 160, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

    // Title
    using var titleFont = new Font(fontFamily, 48, FontStyle.Bold);
    using var titleBrush = new SolidBrush(Color.White);
    g.DrawString(f.Title, titleFont, titleBrush, 640, 300, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

    // Description
    using var descFont = new Font(fontFamily, 24, FontStyle.Regular);
    using var descBrush = new SolidBrush(Color.FromArgb(200, 200, 200));
    g.DrawString(f.Desc, descFont, descBrush, 640, 380, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

    // Details
    using var detailFont = new Font(fontFamily, 18, FontStyle.Regular);
    using var detailBrush = new SolidBrush(Color.FromArgb(0x00, 0x78, 0xD4));
    g.DrawString(f.Details, detailFont, detailBrush, 640, 440, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

    frame.Save(framePath, ImageFormat.Png);
    Console.WriteLine($"[{idx}/{totalFrames}] {f.Title}");
}

// Outro
idx++;
Console.WriteLine($"[{idx}/{totalFrames}] Outro card");
CreateCard("Thank You!", "Download System Sweep Professional", "github.com/naix1337/SystemSweep",
    "⭐ Rate • 🐛 Report • 💡 Request Features", "v2.0", Path.Combine(outputDir, "frame-final.png"), 5);

Console.WriteLine($"\n✅ {totalFrames} frames generated in {outputDir}");

void CreateCard(string title, string subtitle, string url, string tags, string version, string path, int duration)
{
    using var bmp = new Bitmap(1280, 720);
    using var g = Graphics.FromImage(bmp);
    g.Clear(Color.FromArgb(13, 13, 13));

    // Gradient bar
    using var gradBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
        new Point(0, 300), new Point(1280, 420), Color.FromArgb(0, 120, 212), Color.FromArgb(0, 191, 255));
    g.FillRectangle(gradBrush, 0, 300, 1280, 120);

    // Title
    using var titleFont = new Font(fontFamily, 56, FontStyle.Bold);
    using var whiteBrush = new SolidBrush(Color.White);
    g.DrawString(title, titleFont, whiteBrush, 640, 200, new StringFormat { Alignment = StringAlignment.Center });

    // Subtitle
    using var subFont = new Font(fontFamily, 22, FontStyle.Regular);
    using var grayBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
    g.DrawString(subtitle, subFont, grayBrush, 640, 280, new StringFormat { Alignment = StringAlignment.Center });

    // URL
    using var urlFont = new Font(fontFamily, 18, FontStyle.Regular);
    using var blueBrush = new SolidBrush(Color.FromArgb(0, 120, 212));
    g.DrawString(url, urlFont, blueBrush, 640, 480, new StringFormat { Alignment = StringAlignment.Center });

    // Tags
    using var tagFont = new Font(fontFamily, 14, FontStyle.Regular);
    using var dimBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
    g.DrawString(tags, tagFont, dimBrush, 640, 530, new StringFormat { Alignment = StringAlignment.Center });

    // Version badge
    using var verFont = new Font(fontFamily, 12, FontStyle.Bold);
    g.DrawString("v" + version, verFont, blueBrush, 640, 580, new StringFormat { Alignment = StringAlignment.Center });

    bmp.Save(path, ImageFormat.Png);
}
