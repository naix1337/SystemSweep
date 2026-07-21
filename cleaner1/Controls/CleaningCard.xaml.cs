using System.Windows.Controls;
using ModernFileCleaner.Models;

namespace ModernFileCleaner.Controls;

public partial class CleaningCard : UserControl
{
    public CleaningCategory? Category
    {
        get => DataContext as CleaningCategory;
        set => DataContext = value;
    }

    public CleaningCard()
    {
        InitializeComponent();
    }

    public CleaningCard(CleaningCategory category) : this()
    {
        Category = category;
    }
}
