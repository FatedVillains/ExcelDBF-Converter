using System.Windows.Controls;
using ExcelDbfConverter.Desktop.ViewModels;

namespace ExcelDbfConverter.Desktop.Views;

public partial class ExcelToDbfView : UserControl
{
    public ExcelToDbfView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as ExcelToDbfViewModel)?.InitializeAsync();
    }
}
