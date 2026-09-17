using System.Windows.Controls;
using ExcelDbfConverter.Desktop.ViewModels;

namespace ExcelDbfConverter.Desktop.Views;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as HistoryViewModel)?.InitializeAsync();
    }
}
