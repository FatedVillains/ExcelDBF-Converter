using System.Windows.Controls;
using ExcelDbfConverter.Desktop.ViewModels;

namespace ExcelDbfConverter.Desktop.Views;

public partial class DbfToExcelView : UserControl
{
    public DbfToExcelView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as DbfToExcelViewModel)?.InitializeAsync();
    }
}
