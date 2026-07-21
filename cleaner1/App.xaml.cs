using System.Windows;
using ModernFileCleaner.Services;

namespace ModernFileCleaner
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Stellen Sie sicher, dass der Namespace korrekt ist
            AppSettings.Instance.Load();
            ThemeService.SetTheme(AppSettings.Instance.Theme);
        }
    }
}