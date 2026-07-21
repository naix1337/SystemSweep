using System.Windows;

namespace ModernFileCleaner.Pages;

public partial class AboutPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // Navigate back to Clean page
        var mainWindow = (MainWindow)Application.Current.MainWindow;
        mainWindow.NavigateToCleanPage();
    }
}
