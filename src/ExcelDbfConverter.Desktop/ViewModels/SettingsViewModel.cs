using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelDbfConverter.Core.Constants;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Desktop.Abstractions;

namespace ExcelDbfConverter.Desktop.ViewModels;

/// <summary>系统设置页面 ViewModel。</summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly Infrastructure.Configuration.AppPaths _appPaths;

    public SettingsViewModel(ISettingsService settingsService, IDialogService dialogService, Infrastructure.Configuration.AppPaths appPaths)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
        _appPaths = appPaths;

        ExcelFormats = Enum.GetValues<ExcelFormat>();
        Encodings = Enum.GetValues<DbfEncodingKind>();
        PreviewOptions = AppDefaults.PreviewRowOptions;
        Keywords = new ObservableCollection<string>();

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        BrowseOutputDirCommand = new RelayCommand(BrowseOutputDir);
        AddKeywordCommand = new RelayCommand(AddKeyword);
        RemoveKeywordCommand = new RelayCommand<string?>(RemoveKeyword);
        ResetKeywordsCommand = new RelayCommand(ResetKeywords);
    }

    [ObservableProperty]
    private string _defaultOutputDirectory = string.Empty;

    [ObservableProperty]
    private ExcelFormat _defaultExcelFormat = ExcelFormat.Xlsx;

    [ObservableProperty]
    private DbfEncodingKind _defaultDbfEncoding = DbfEncodingKind.Auto;

    [ObservableProperty]
    private int _previewRowCount = AppDefaults.DefaultPreviewRows;

    [ObservableProperty]
    private string _newKeyword = string.Empty;

    [ObservableProperty]
    private string? _selectedKeyword;

    public ObservableCollection<string> Keywords { get; }

    public IReadOnlyList<ExcelFormat> ExcelFormats { get; }

    public IReadOnlyList<DbfEncodingKind> Encodings { get; }

    public IReadOnlyList<int> PreviewOptions { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand BrowseOutputDirCommand { get; }

    public IRelayCommand AddKeywordCommand { get; }

    public IRelayCommand<string?> RemoveKeywordCommand { get; }

    public IRelayCommand ResetKeywordsCommand { get; }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadWithDefaultsAsync();
        DefaultOutputDirectory = string.IsNullOrWhiteSpace(settings.DefaultOutputDirectory)
            ? _appPaths.DataDirectory
            : settings.DefaultOutputDirectory;
        DefaultExcelFormat = settings.DefaultExcelFormat;
        DefaultDbfEncoding = settings.DefaultDbfEncoding;
        PreviewRowCount = settings.PreviewRowCount;

        Keywords.Clear();
        foreach (var keyword in settings.TextKeywords)
        {
            Keywords.Add(keyword);
        }
    }

    private async Task SaveAsync()
    {
        var settings = new AppSettings
        {
            DefaultOutputDirectory = DefaultOutputDirectory,
            DefaultExcelFormat = DefaultExcelFormat,
            DefaultDbfEncoding = DefaultDbfEncoding,
            PreviewRowCount = PreviewRowCount,
            TextKeywords = Keywords.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList(),
        };

        await _settingsService.SaveAsync(settings);
        _dialogService.ShowMessage("保存成功", "设置已保存。");
    }

    private void BrowseOutputDir()
    {
        string? dir = _dialogService.OpenFolder("选择默认输出目录");
        if (dir is not null)
        {
            DefaultOutputDirectory = dir;
        }
    }

    private void AddKeyword()
    {
        string keyword = NewKeyword?.Trim() ?? string.Empty;
        if (keyword.Length > 0 && !Keywords.Contains(keyword))
        {
            Keywords.Add(keyword);
        }
        NewKeyword = string.Empty;
    }

    private void RemoveKeyword(string? keyword)
    {
        if (keyword is not null)
        {
            Keywords.Remove(keyword);
        }
    }

    private void ResetKeywords()
    {
        Keywords.Clear();
        foreach (var keyword in AppDefaults.DefaultTextKeywords)
        {
            Keywords.Add(keyword);
        }
    }
}
