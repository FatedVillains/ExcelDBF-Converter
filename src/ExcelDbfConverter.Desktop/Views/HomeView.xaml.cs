using System.Windows.Controls;
using ExcelDbfConverter.Desktop.ViewModels;

namespace ExcelDbfConverter.Desktop.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as HomeViewModel)?.RefreshAsync();
    }
}
