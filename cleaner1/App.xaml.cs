using System.Windows;

namespace ModernFileCleaner
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Stellen Sie sicher, dass der Namespace korrekt ist
            AppSettings.Instance.Load();
        }
    }
}