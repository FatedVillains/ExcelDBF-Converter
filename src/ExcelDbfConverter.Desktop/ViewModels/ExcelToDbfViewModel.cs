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

/// <summary>Excel → DBF 页面 ViewModel。</summary>
public sealed partial class ExcelToDbfViewModel : ViewModelBase
{
    private readonly IExcelReader _excelReader;
    private readonly IFieldTypeDetector _fieldDetector;
    private readonly ITemplateService _templateService;
    private readonly IFieldNameValidator _fieldNameValidator;
    private readonly ISettingsService _settingsService;
    private readonly IConversionService _conversionService;
    private readonly IDialogService _dialogService;

    private CancellationTokenSource? _cts;
    private List<FieldMapping> _lastAutoDetected = new();

    public ExcelToDbfViewModel(
        IExcelReader excelReader,
        IFieldTypeDetector fieldDetector,
        ITemplateService templateService,
        IFieldNameValidator fieldNameValidator,
        ISettingsService settingsService,
        IConversionService conversionService,
        IDialogService dialogService)
    {
        _excelReader = excelReader;
        _fieldDetector = fieldDetector;
        _templateService = templateService;
        _fieldNameValidator = fieldNameValidator;
        _settingsService = settingsService;
        _conversionService = conversionService;
        _dialogService = dialogService;

        Sheets = new ObservableCollection<ExcelSheetInfo>();
        Mappings = new ObservableCollection<FieldMappingViewModel>();
        Templates = new ObservableCollection<TemplateInfo>();
        Encodings = Enum.GetValues<DbfEncodingKind>();
        ErrorHandlingOptions = Enum.GetValues<ErrorHandlingMode>();
        FieldTypes = FieldMappingViewModel.SupportedTypes;

        PreviewRowCountOptions = Core.Constants.AppDefaults.PreviewRowOptions;

        BrowseCommand = new AsyncRelayCommand(BrowseAsync);
        LoadFileCommand = new AsyncRelayCommand<string?>(LoadFileAsync);
        ApplyTemplateCommand = new AsyncRelayCommand(ApplyTemplateAsync);
        ResetDetectCommand = new AsyncRelayCommand(ResetDetectAsync);
        AddFieldCommand = new RelayCommand(AddField);
        DeleteFieldCommand = new RelayCommand<FieldMappingViewModel?>(DeleteField);
        MoveUpCommand = new RelayCommand<FieldMappingViewModel?>(MoveUp);
        MoveDownCommand = new RelayCommand<FieldMappingViewModel?>(MoveDown);
        SelectAllCommand = new RelayCommand(() => SetAllExport(true));
        DeselectAllCommand = new RelayCommand(() => SetAllExport(false));
        BrowseOutputCommand = new RelayCommand(BrowseOutput);
        ConvertCommand = new AsyncRelayCommand(ConvertAsync, () => !IsConverting);
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsConverting);
    }

    [ObservableProperty]
    private string _sourcePath = string.Empty;

    [ObservableProperty]
    private ExcelSheetInfo? _selectedSheet;

    [ObservableProperty]
    private DataTable? _previewTable;

    [ObservableProperty]
    private string _previewInfo = string.Empty;

    [ObservableProperty]
    private FieldMappingViewModel? _selectedMapping;

    [ObservableProperty]
    private TemplateInfo? _selectedTemplate;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private DbfEncodingKind _selectedEncoding = DbfEncodingKind.Gbk;

    [ObservableProperty]
    private ErrorHandlingMode _selectedErrorHandling = ErrorHandlingMode.Stop;

    [ObservableProperty]
    private int _selectedPreviewRowCount = Core.Constants.AppDefaults.DefaultPreviewRows;

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<ExcelSheetInfo> Sheets { get; }

    public ObservableCollection<FieldMappingViewModel> Mappings { get; }

    public ObservableCollection<TemplateInfo> Templates { get; }

    public IReadOnlyList<DbfEncodingKind> Encodings { get; }

    public IReadOnlyList<ErrorHandlingMode> ErrorHandlingOptions { get; }

    public IReadOnlyList<DbfFieldType> FieldTypes { get; }

    public IReadOnlyList<int> PreviewRowCountOptions { get; }

    public IAsyncRelayCommand BrowseCommand { get; }

    public IAsyncRelayCommand<string?> LoadFileCommand { get; }

    public IAsyncRelayCommand ApplyTemplateCommand { get; }

    public IAsyncRelayCommand ResetDetectCommand { get; }

    public IRelayCommand AddFieldCommand { get; }

    public IRelayCommand<FieldMappingViewModel?> DeleteFieldCommand { get; }

    public IRelayCommand<FieldMappingViewModel?> MoveUpCommand { get; }

    public IRelayCommand<FieldMappingViewModel?> MoveDownCommand { get; }

    public IRelayCommand SelectAllCommand { get; }

    public IRelayCommand DeselectAllCommand { get; }

    public IRelayCommand BrowseOutputCommand { get; }

    public IAsyncRelayCommand ConvertCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadWithDefaultsAsync();
        SelectedEncoding = settings.DefaultDbfEncoding == DbfEncodingKind.Auto ? DbfEncodingKind.Gbk : settings.DefaultDbfEncoding;
        SelectedPreviewRowCount = settings.PreviewRowCount;
        SelectedErrorHandling = ErrorHandlingMode.Stop;

        var templates = await _templateService.GetAllAsync();
        Templates.Clear();
        Templates.Add(new TemplateInfo { Name = "不使用模板" });
        foreach (var t in templates)
        {
            Templates.Add(t);
        }
        SelectedTemplate = Templates[0];
    }

    private async Task BrowseAsync()
    {
        string? path = _dialogService.OpenFile("选择 Excel 文件", "Excel 文件|*.xlsx;*.xls");
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
        try
        {
            var sheets = await _excelReader.ReadSheetsAsync(path);
            Sheets.Clear();
            foreach (var sheet in sheets)
            {
                Sheets.Add(sheet);
            }
            SelectedSheet = Sheets.FirstOrDefault();
            if (SelectedSheet is null)
            {
                StatusMessage = "未找到任何 Sheet。";
                return;
            }
            await LoadPreviewAndDetectAsync();
            StatusMessage = "文件读取完成。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"读取失败：{ex.Message}";
        }
    }

    private async Task LoadPreviewAndDetectAsync()
    {
        if (SelectedSheet is null)
        {
            return;
        }

        var preview = await _excelReader.ReadPreviewAsync(SourcePath, SelectedSheet.Name, SelectedPreviewRowCount);
        PreviewTable = BuildPreviewTable(preview);
        PreviewInfo = $"共 {preview.TotalRows} 行，{preview.Columns.Count} 列；预览前 {preview.Rows.Count} 行。";

        var settings = await _settingsService.LoadWithDefaultsAsync();
        var mappings = _fieldDetector.Detect(preview.Columns, preview.Rows, settings.TextKeywords);
        _lastAutoDetected = mappings.ToList();
        PopulateMappings(mappings);

        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            OutputPath = Path.Combine(Path.GetDirectoryName(SourcePath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(SourcePath) + ".dbf");
        }
    }

    private void PopulateMappings(IReadOnlyList<FieldMapping> mappings)
    {
        Mappings.Clear();
        foreach (var mapping in mappings)
        {
            Mappings.Add(FieldMappingViewModel.FromMapping(mapping));
        }
    }

    private static DataTable BuildPreviewTable(ExcelPreviewResult preview)
    {
        var table = new DataTable();
        foreach (var column in preview.Columns)
        {
            table.Columns.Add(column.Name, typeof(string));
        }
        foreach (var row in preview.Rows)
        {
            var values = new object?[preview.Columns.Count];
            for (int c = 0; c < preview.Columns.Count; c++)
            {
                values[c] = c < row.Values.Count ? row.Values[c]?.ToString() : string.Empty;
            }
            table.Rows.Add(values);
        }
        return table;
    }

    private async Task ApplyTemplateAsync()
    {
        if (SelectedTemplate is null || SelectedTemplate.Id == 0 || _lastAutoDetected.Count == 0)
        {
            return;
        }

        var (mappings, missing, unused) = _templateService.ApplyTemplate(SelectedTemplate, _lastAutoDetected);
        PopulateMappings(mappings);

        var message = new System.Text.StringBuilder();
        if (missing.Count > 0)
        {
            message.AppendLine("Excel 中未找到字段：" + string.Join("、", missing));
        }
        if (unused.Count > 0)
        {
            message.AppendLine("以下字段未使用：" + string.Join("、", unused));
        }
        if (message.Length > 0)
        {
            StatusMessage = message.ToString().Trim();
        }
    }

    private async Task ResetDetectAsync()
    {
        await LoadPreviewAndDetectAsync();
    }

    private void AddField()
    {
        Mappings.Add(new FieldMappingViewModel { ExcelColumnName = "(新增)", DbfFieldName = "NEWFIELD", IsExport = true });
    }

    private void DeleteField(FieldMappingViewModel? item)
    {
        if (item is not null)
        {
            Mappings.Remove(item);
        }
    }

    private void MoveUp(FieldMappingViewModel? item)
    {
        int index = item is null ? -1 : Mappings.IndexOf(item);
        if (index > 0)
        {
            Mappings.Move(index, index - 1);
        }
    }

    private void MoveDown(FieldMappingViewModel? item)
    {
        int index = item is null ? -1 : Mappings.IndexOf(item);
        if (index >= 0 && index < Mappings.Count - 1)
        {
            Mappings.Move(index, index + 1);
        }
    }

    private void SetAllExport(bool value)
    {
        foreach (var mapping in Mappings)
        {
            mapping.IsExport = value;
        }
    }

    private void BrowseOutput()
    {
        string? path = _dialogService.SaveFile("选择输出位置", Path.GetFileName(OutputPath), "DBF 文件|*.dbf");
        if (path is not null)
        {
            OutputPath = path;
        }
    }

    private async Task ConvertAsync()
    {
        // 字段名校验。
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var exported = Mappings.Where(m => m.IsExport).ToList();
        if (exported.Count == 0)
        {
            _dialogService.ShowMessage("提示", "请至少选择一个要导出的字段。");
            return;
        }

        foreach (var mapping in exported)
        {
            var validation = _fieldNameValidator.Validate(mapping.DbfFieldName, names);
            if (!validation.IsValid)
            {
                _dialogService.ShowMessage("字段名称错误", $"字段「{mapping.DbfFieldName}」：{validation.Message}");
                return;
            }
            names.Add(mapping.DbfFieldName);
        }

        var request = new ExcelToDbfRequest
        {
            SourcePath = SourcePath,
            SheetName = SelectedSheet?.Name ?? string.Empty,
            OutputPath = OutputPath,
            FieldMappings = exported.Select(m => m.ToMapping()).ToList(),
            HasHeaderRow = true,
            Encoding = SelectedEncoding,
            ErrorHandling = SelectedErrorHandling,
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

            var result = await _conversionService.ConvertExcelToDbfAsync(request, progress, _cts.Token);

            if (result.Status == ConversionStatus.Cancelled)
            {
                StatusMessage = "转换已取消。";
            }
            else if (result.Status == ConversionStatus.Success || result.Status == ConversionStatus.Partial)
            {
                ProgressPercentage = 100;
                StatusMessage = $"转换完成：成功 {result.SuccessRows} 行，失败 {result.FailedRows} 行。输出：{result.OutputFilePath}";
                if (!string.IsNullOrEmpty(result.ErrorReportPath))
                {
                    StatusMessage += $"\n错误报告：{result.ErrorReportPath}";
                }
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

    partial void OnSelectedSheetChanged(ExcelSheetInfo? value)
    {
        if (value is not null && !string.IsNullOrWhiteSpace(SourcePath))
        {
            _ = LoadPreviewAndDetectAsync();
        }
    }
}
