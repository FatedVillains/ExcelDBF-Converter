using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelDbfConverter.Core.DTOs;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Desktop.Abstractions;

namespace ExcelDbfConverter.Desktop.ViewModels;

/// <summary>DBF → Excel 页面 ViewModel。</summary>
public sealed partial class DbfToExcelViewModel : ViewModelBase
{
    private readonly IDbfReader _dbfReader;
    private readonly ISettingsService _settingsService;
    private readonly IConversionService _conversionService;
    private readonly IDialogService _dialogService;

    private CancellationTokenSource? _cts;

    public DbfToExcelViewModel(
        IDbfReader dbfReader,
        ISettingsService settingsService,
        IConversionService conversionService,
        IDialogService dialogService)
    {
        _dbfReader = dbfReader;
        _settingsService = settingsService;
        _conversionService = conversionService;
        _dialogService = dialogService;

        Fields = new ObservableCollection<DbfFieldDefinition>();
        Encodings = Enum.GetValues<DbfEncodingKind>();
        ExcelFormats = Enum.GetValues<ExcelFormat>();

        BrowseCommand = new AsyncRelayCommand(BrowseAsync);
        LoadFileCommand = new AsyncRelayCommand<string?>(LoadFileAsync);
        ReloadCommand = new AsyncRelayCommand(ReloadAsync);
        BrowseOutputCommand = new RelayCommand(BrowseOutput);
        ConvertCommand = new AsyncRelayCommand(ConvertAsync, () => !IsConverting);
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsConverting);
    }

    [ObservableProperty]
    private string _sourcePath = string.Empty;

    [ObservableProperty]
    private DataTable? _previewTable;

    [ObservableProperty]
    private string _previewInfo = string.Empty;

    [ObservableProperty]
    private DbfEncodingKind _selectedEncoding = DbfEncodingKind.Auto;

    [ObservableProperty]
    private ExcelFormat _selectedExcelFormat = ExcelFormat.Xlsx;

    [ObservableProperty]
    private string _sheetName = "数据导出";

    [ObservableProperty]
    private int _maxRowsPerSheet = 500_000;

    [ObservableProperty]
    private bool _includeDeleted;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<DbfFieldDefinition> Fields { get; }

    public IReadOnlyList<DbfEncodingKind> Encodings { get; }

    public IReadOnlyList<ExcelFormat> ExcelFormats { get; }

    public IAsyncRelayCommand BrowseCommand { get; }

    public IAsyncRelayCommand<string?> LoadFileCommand { get; }

    public IAsyncRelayCommand ReloadCommand { get; }

    public IRelayCommand BrowseOutputCommand { get; }

    public IAsyncRelayCommand ConvertCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadWithDefaultsAsync();
        SelectedEncoding = settings.DefaultDbfEncoding;
        SelectedExcelFormat = settings.DefaultExcelFormat;
    }

    private async Task BrowseAsync()
    {
        string? path = _dialogService.OpenFile("选择 DBF 文件", "DBF 文件|*.dbf");
        if (path is not null)
        {
            await LoadFileAsync(path);
        }
    }

    public async Task LoadFileAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            StatusMessage = "文件不存在。";
            return;
        }

        SourcePath = path;
        await LoadStructureAndPreviewAsync();

        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            OutputPath = Path.Combine(Path.GetDirectoryName(SourcePath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(SourcePath) + ".xlsx");
        }
    }

    private async Task ReloadAsync()
    {
        await LoadStructureAndPreviewAsync();
    }

    private async Task LoadStructureAndPreviewAsync()
    {
        try
        {
            var info = await _dbfReader.ReadStructureAsync(SourcePath);
            Fields.Clear();
            foreach (var field in info.Schema.Fields)
            {
                Fields.Add(field);
            }

            var options = new DbfReadOptions
            {
                Encoding = SelectedEncoding,
                IncludeDeleted = true,
                MaxRecords = Core.Constants.AppDefaults.DefaultPreviewRows,
            };

            var preview = new DataTable();
            foreach (var field in info.Schema.Fields)
            {
                preview.Columns.Add(field.Name, typeof(string));
            }

            int count = 0;
            await foreach (var record in _dbfReader.ReadRecordsAsync(SourcePath, options))
            {
                var values = new object?[info.Schema.Fields.Count];
                for (int i = 0; i < info.Schema.Fields.Count; i++)
                {
                    values[i] = i < record.Values.Count ? record.Values[i]?.ToString() : string.Empty;
                }
                preview.Rows.Add(values);
                count++;
            }

            PreviewTable = preview;
            PreviewInfo = $"共 {info.RecordCount} 条记录，{info.Schema.Fields.Count} 个字段；预览前 {count} 条。"
                + (info.HasMemoField ? " 该文件含 Memo 字段（第一版不读取 FPT）。" : string.Empty);
            StatusMessage = "读取完成。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"读取失败：{ex.Message}";
        }
    }

    private void BrowseOutput()
    {
        string ext = SelectedExcelFormat.FileExtension();
        string? path = _dialogService.SaveFile("选择输出位置", Path.GetFileName(OutputPath), $"Excel 文件|*{ext}");
        if (path is not null)
        {
            OutputPath = path;
        }
    }

    private async Task ConvertAsync()
    {
        if (string.IsNullOrWhiteSpace(SheetName))
        {
            _dialogService.ShowMessage("提示", "Sheet 名称不能为空。");
            return;
        }

        var request = new DbfToExcelRequest
        {
            SourcePath = SourcePath,
            OutputPath = OutputPath,
            ExcelFormat = SelectedExcelFormat,
            SheetName = SheetName,
            MaxRowsPerSheet = MaxRowsPerSheet,
            Encoding = SelectedEncoding,
            IncludeDeleted = IncludeDeleted,
        };

        _cts = new CancellationTokenSource();
        IsConverting = true;
        ProgressPercentage = 0;
        ProgressText = "准备转换…";
        ConvertCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();

        try
        {
            var progress = new Progress<ConversionProgress>(p =>
            {
                ProgressPercentage = p.Percentage;
                ProgressText = $"正在转换 {p.CurrentFileName}：{p.CurrentRow} / {p.TotalRows} 行 ({p.Percentage:F0}%)";
            });

            var result = await _conversionService.ConvertDbfToExcelAsync(request, progress, _cts.Token);

            if (result.Status == ConversionStatus.Cancelled)
            {
                StatusMessage = "转换已取消。";
            }
            else if (result.Status == ConversionStatus.Success)
            {
                ProgressPercentage = 100;
                StatusMessage = $"转换完成：{result.SuccessRows} 行。输出：{result.OutputFilePath}";
                _dialogService.ShowMessage("转换完成", StatusMessage);
            }
            else
            {
                StatusMessage = $"转换失败：{result.Message}";
                _dialogService.ShowMessage("转换失败", result.Message ?? "未知错误");
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "转换已取消。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"转换失败：{ex.Message}";
            _dialogService.ShowMessage("转换失败", ex.Message);
        }
        finally
        {
            IsConverting = false;
            _cts.Dispose();
            _cts = null;
            ConvertCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSelectedEncodingChanged(DbfEncodingKind value)
    {
        if (!string.IsNullOrWhiteSpace(SourcePath))
        {
            _ = ReloadAsync();
        }
    }
}
