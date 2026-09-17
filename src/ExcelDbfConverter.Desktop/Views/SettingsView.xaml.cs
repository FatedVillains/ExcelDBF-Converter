using System.Windows.Controls;
using ExcelDbfConverter.Desktop.ViewModels;

namespace ExcelDbfConverter.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as SettingsViewModel)?.InitializeAsync();
    }
}
