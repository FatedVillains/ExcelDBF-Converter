using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Desktop.Abstractions;

namespace ExcelDbfConverter.Desktop.ViewModels;

/// <summary>转换历史页面 ViewModel。</summary>
public sealed partial class HistoryViewModel : ViewModelBase
{
    private readonly IHistoryService _historyService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigation;
    private readonly ExcelToDbfViewModel _excelToDbf;
    private readonly DbfToExcelViewModel _dbfToExcel;

    public HistoryViewModel(
        IHistoryService historyService,
        IDialogService dialogService,
        INavigationService navigation,
        ExcelToDbfViewModel excelToDbf,
        DbfToExcelViewModel dbfToExcel)
    {
        _historyService = historyService;
        _dialogService = dialogService;
        _navigation = navigation;
        _excelToDbf = excelToDbf;
        _dbfToExcel = dbfToExcel;

        Entries = new ObservableCollection<ConversionHistoryEntry>();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        DeleteCommand = new AsyncRelayCommand<ConversionHistoryEntry?>(DeleteAsync);
        ClearCommand = new AsyncRelayCommand(ClearAsync);
        OpenFolderCommand = new RelayCommand<ConversionHistoryEntry?>(OpenFolder);
        ReopenFileCommand = new AsyncRelayCommand<ConversionHistoryEntry?>(ReopenFileAsync);
    }

    public ObservableCollection<ConversionHistoryEntry> Entries { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand<ConversionHistoryEntry?> DeleteCommand { get; }

    public IAsyncRelayCommand ClearCommand { get; }

    public IRelayCommand<ConversionHistoryEntry?> OpenFolderCommand { get; }

    public IAsyncRelayCommand<ConversionHistoryEntry?> ReopenFileCommand { get; }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        var entries = await _historyService.GetAllAsync();
        Entries.Clear();
        foreach (var e in entries)
        {
            Entries.Add(e);
        }
    }

    private async Task DeleteAsync(ConversionHistoryEntry? entry)
    {
        if (entry is null)
        {
            return;
        }
        await _historyService.DeleteAsync(entry.Id);
        await RefreshAsync();
    }

    private async Task ClearAsync()
    {
        if (!_dialogService.ShowConfirm("确认", "确定清空全部转换历史吗？"))
        {
            return;
        }
        await _historyService.ClearAsync();
        await RefreshAsync();
    }

    private void OpenFolder(ConversionHistoryEntry? entry)
    {
        if (entry is null)
        {
            return;
        }
        string path = !string.IsNullOrWhiteSpace(entry.TargetFilePath)
            ? entry.TargetFilePath
            : entry.SourceFilePath;
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
    }

    private async Task ReopenFileAsync(ConversionHistoryEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        if (!File.Exists(entry.SourceFilePath))
        {
            _dialogService.ShowMessage("提示", $"源文件不存在：{entry.SourceFilePath}");
            return;
        }

        switch (entry.ConversionType)
        {
            case Core.Enums.ConversionType.ExcelToDbf:
                _navigation.CurrentViewModel = _excelToDbf;
                await _excelToDbf.LoadFileAsync(entry.SourceFilePath);
                break;
            case Core.Enums.ConversionType.DbfToExcel:
                _navigation.CurrentViewModel = _dbfToExcel;
                await _dbfToExcel.LoadFileAsync(entry.SourceFilePath);
                break;
            default:
                _dialogService.ShowMessage("提示", "无法识别的转换类型。");
                break;
        }
    }
}
