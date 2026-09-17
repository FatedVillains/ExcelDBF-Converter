using System.Windows.Controls;
using ExcelDbfConverter.Desktop.ViewModels;

namespace ExcelDbfConverter.Desktop.Views;

public partial class TemplateManagementView : UserControl
{
    public TemplateManagementView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (DataContext as TemplateManagementViewModel)?.InitializeAsync();
    }
}
