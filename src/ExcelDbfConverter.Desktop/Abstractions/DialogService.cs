using Microsoft.Win32;

namespace ExcelDbfConverter.Desktop.Abstractions;

/// <summary>文件/目录对话框服务。</summary>
public interface IDialogService
{
    string? OpenFile(string title, string filter);

    IReadOnlyList<string> OpenFiles(string title, string filter);

    string? SaveFile(string title, string defaultFileName, string filter);

    string? OpenFolder(string title);

    void ShowMessage(string title, string message);

    bool ShowConfirm(string title, string message);
}

/// <summary>基于 WPF 对话框的实现。</summary>
public sealed class DialogService : IDialogService
{
    public string? OpenFile(string title, string filter)
    {
        var dialog = new OpenFileDialog { Title = title, Filter = filter };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public IReadOnlyList<string> OpenFiles(string title, string filter)
    {
        var dialog = new OpenFileDialog { Title = title, Filter = filter, Multiselect = true };
        return dialog.ShowDialog() == true ? dialog.FileNames : Array.Empty<string>();
    }

    public string? SaveFile(string title, string defaultFileName, string filter)
    {
        var dialog = new SaveFileDialog { Title = title, FileName = defaultFileName, Filter = filter };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? OpenFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public void ShowMessage(string title, string message)
    {
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    public bool ShowConfirm(string title, string message)
    {
        return System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
    }
}
