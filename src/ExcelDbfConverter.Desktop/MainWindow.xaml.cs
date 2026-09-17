using System.Windows;
using ExcelDbfConverter.Desktop.ViewModels;

namespace ExcelDbfConverter.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
