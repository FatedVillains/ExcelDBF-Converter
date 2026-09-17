using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelDbfConverter.Core.DTOs;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Desktop.Abstractions;

namespace ExcelDbfConverter.Desktop.ViewModels;

/// <summary>批量转换页面 ViewModel。</summary>
public sealed partial class BatchConversionViewModel : ViewModelBase
{
    private readonly IConversionService _conversionService;
    private readonly IDialogService _dialogService;

    private CancellationTokenSource? _cts;

    public BatchConversionViewModel(IConversionService conversionService, IDialogService dialogService)
    {
        _conversionService = conversionService;
        _dialogService = dialogService;

        Results = new ObservableCollection<BatchItemResult>();
        SourceFiles = new ObservableCollection<string>();
        ConversionTypes = Enum.GetValues<ConversionType>();
        OverwriteModes = Enum.GetValues<OverwriteMode>();
        ErrorHandlingOptions = Enum.GetValues<ErrorHandlingMode>();
        ExcelFormats = Enum.GetValues<ExcelFormat>();
        Encodings = Enum.GetValues<DbfEncodingKind>();

        AddFilesCommand = new RelayCommand(AddFiles);
        ClearFilesCommand = new RelayCommand(() => SourceFiles.Clear());
        BrowseSourceDirCommand = new RelayCommand(BrowseSourceDir);
        BrowseOutputDirCommand = new RelayCommand(BrowseOutputDir);
        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsConverting);
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsConverting);
        ClearResultsCommand = new RelayCommand(() => Results.Clear());
    }

    [ObservableProperty]
    private ConversionType _selectedConversionType = ConversionType.ExcelToDbf;

    [ObservableProperty]
    private string _sourceDirectory = string.Empty;

    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private bool _includeSubdirectories;

    [ObservableProperty]
    private OverwriteMode _selectedOverwriteMode = OverwriteMode.Rename;

    [ObservableProperty]
    private ErrorHandlingMode _selectedErrorHandling = ErrorHandlingMode.Stop;

    [ObservableProperty]
    private ExcelFormat _selectedExcelFormat = ExcelFormat.Xlsx;

    [ObservableProperty]
    private DbfEncodingKind _selectedEncoding = DbfEncodingKind.Auto;

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _summary = string.Empty;

    public ObservableCollection<string> SourceFiles { get; }

    public ObservableCollection<BatchItemResult> Results { get; }

    public IReadOnlyList<ConversionType> ConversionTypes { get; }

    public IReadOnlyList<OverwriteMode> OverwriteModes { get; }

    public IReadOnlyList<ErrorHandlingMode> ErrorHandlingOptions { get; }

    public IReadOnlyList<ExcelFormat> ExcelFormats { get; }

    public IReadOnlyList<DbfEncodingKind> Encodings { get; }

    public IRelayCommand AddFilesCommand { get; }

    public IRelayCommand ClearFilesCommand { get; }

    public IRelayCommand BrowseSourceDirCommand { get; }

    public IRelayCommand BrowseOutputDirCommand { get; }

    public IAsyncRelayCommand StartCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand ClearResultsCommand { get; }

    private void AddFiles()
    {
        string filter = SelectedConversionType == ConversionType.ExcelToDbf
            ? "Excel 文件|*.xlsx;*.xls"
            : "DBF 文件|*.dbf";
        var files = _dialogService.OpenFiles("选择文件", filter);
        foreach (var file in files)
        {
            if (!SourceFiles.Contains(file))
            {
                SourceFiles.Add(file);
            }
        }
    }

    private void BrowseSourceDir()
    {
        string? dir = _dialogService.OpenFolder("选择源目录");
        if (dir is not null)
        {
            SourceDirectory = dir;
        }
    }

    private void BrowseOutputDir()
    {
        string? dir = _dialogService.OpenFolder("选择输出目录");
        if (dir is not null)
        {
            OutputDirectory = dir;
        }
    }

    private async Task StartAsync()
    {
        if (SourceFiles.Count == 0 && string.IsNullOrWhiteSpace(SourceDirectory))
        {
            _dialogService.ShowMessage("提示", "请选择源文件或源目录。");
            return;
        }
        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            _dialogService.ShowMessage("提示", "请选择输出目录。");
            return;
        }

        var request = new BatchConversionRequest
        {
            ConversionType = SelectedConversionType,
            SourceFiles = SourceFiles.ToList(),
            SourceDirectory = SourceDirectory,
            OutputDirectory = OutputDirectory,
            IncludeSubdirectories = IncludeSubdirectories,
            OverwriteMode = SelectedOverwriteMode,
            ErrorHandling = SelectedErrorHandling,
            ExcelFormat = SelectedExcelFormat,
            Encoding = SelectedEncoding,
        };

        _cts = new CancellationTokenSource();
        IsConverting = true;
        Results.Clear();
        ProgressPercentage = 0;
        StartCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var progress = new Progress<ConversionProgress>(p =>
            {
                ProgressPercentage = p.Percentage;
                ProgressText = $"正在处理 {p.CurrentFileName}（{p.CurrentRow}/{p.TotalRows}）";
            });

            var results = await _conversionService.BatchConvertAsync(request, progress, _cts.Token);
            foreach (var item in results)
            {
                Results.Add(item);
            }

            int success = results.Count(r => r.Status == ConversionStatus.Success);
            int failed = results.Count - success;
            Summary = $"总文件：{results.Count}，成功：{success}，失败：{failed}，耗时：{stopwatch.Elapsed:hh\\:mm\\:ss}";
        }
        catch (OperationCanceledException)
        {
            Summary = "批量转换已取消。";
        }
        catch (Exception ex)
        {
            Summary = $"批量转换失败：{ex.Message}";
            _dialogService.ShowMessage("转换失败", ex.Message);
        }
        finally
        {
            IsConverting = false;
            _cts.Dispose();
            _cts = null;
            StartCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
    }
}
