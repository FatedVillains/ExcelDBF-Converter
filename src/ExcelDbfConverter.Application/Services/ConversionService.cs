using System.Diagnostics;
using ExcelDbfConverter.Core.Constants;
using ExcelDbfConverter.Core.DTOs;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Application.Services;

/// <summary>转换服务：单文件与批量转换编排。</summary>
public sealed class ConversionService : IConversionService
{
    private readonly IConversionTask<ExcelToDbfRequest> _excelToDbf;
    private readonly IConversionTask<DbfToExcelRequest> _dbfToExcel;
    private readonly IHistoryService _history;
    private readonly IExcelReader _excelReader;
    private readonly IFieldTypeDetector _fieldDetector;
    private readonly ISettingsService _settingsService;

    public ConversionService(
        IConversionTask<ExcelToDbfRequest> excelToDbf,
        IConversionTask<DbfToExcelRequest> dbfToExcel,
        IHistoryService history,
        IExcelReader excelReader,
        IFieldTypeDetector fieldDetector,
        ISettingsService settingsService)
    {
        _excelToDbf = excelToDbf;
        _dbfToExcel = dbfToExcel;
        _history = history;
        _excelReader = excelReader;
        _fieldDetector = fieldDetector;
        _settingsService = settingsService;
    }

    public async Task<ConversionResult> ConvertExcelToDbfAsync(
        ExcelToDbfRequest request,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _excelToDbf.ExecuteAsync(request, progress, cancellationToken).ConfigureAwait(false);
        await RecordHistoryAsync(
            ConversionType.ExcelToDbf,
            request.SourcePath,
            result.OutputFilePath,
            result,
            cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<ConversionResult> ConvertDbfToExcelAsync(
        DbfToExcelRequest request,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _dbfToExcel.ExecuteAsync(request, progress, cancellationToken).ConfigureAwait(false);
        await RecordHistoryAsync(
            ConversionType.DbfToExcel,
            request.SourcePath,
            result.OutputFilePath,
            result,
            cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<IReadOnlyList<BatchItemResult>> BatchConvertAsync(
        BatchConversionRequest request,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var files = CollectFiles(request);
        var results = new List<BatchItemResult>(files.Count);
        int processed = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string targetPath = BuildTargetPath(file, request);
            var item = new BatchItemResult
            {
                FileName = Path.GetFileName(file),
                SourcePath = file,
            };

            try
            {
                if (File.Exists(targetPath) && request.OverwriteMode == OverwriteMode.Skip)
                {
                    item.Status = ConversionStatus.Failed;
                    item.Message = "目标文件已存在，跳过";
                    results.Add(item);
                    continue;
                }

                ConversionResult singleResult;
                if (request.ConversionType == ConversionType.ExcelToDbf)
                {
                    var mappings = await AutoDetectMappingsAsync(file, cancellationToken).ConfigureAwait(false);
                    singleResult = await _excelToDbf.ExecuteAsync(new ExcelToDbfRequest
                    {
                        SourcePath = file,
                        SheetName = await GetFirstSheetAsync(file, cancellationToken).ConfigureAwait(false),
                        OutputPath = targetPath,
                        FieldMappings = mappings,
                        HasHeaderRow = true,
                        Encoding = request.Encoding,
                        ErrorHandling = request.ErrorHandling,
                    }, progress, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    singleResult = await _dbfToExcel.ExecuteAsync(new DbfToExcelRequest
                    {
                        SourcePath = file,
                        OutputPath = targetPath,
                        ExcelFormat = request.ExcelFormat,
                        SheetName = ExcelConstants.DefaultSheetName,
                        Encoding = request.Encoding,
                    }, progress, cancellationToken).ConfigureAwait(false);
                }

                item.TargetPath = singleResult.OutputFilePath;
                item.Status = singleResult.Status;
                item.Message = singleResult.Status == ConversionStatus.Success
                    ? "转换完成"
                    : singleResult.Message ?? "转换失败";
            }
            catch (OperationCanceledException)
            {
                item.Status = ConversionStatus.Cancelled;
                item.Message = "已取消";
            }
            catch (Exception ex)
            {
                item.Status = ConversionStatus.Failed;
                item.Message = ex.Message;
            }

            results.Add(item);
            processed++;

            progress?.Report(new ConversionProgress
            {
                CurrentFileName = Path.GetFileName(file),
                CurrentRow = processed,
                TotalRows = files.Count,
                Percentage = files.Count > 0 ? (double)processed / files.Count * 100 : 0,
                Elapsed = stopwatch.Elapsed,
                EstimatedRemaining = processed > 0 && files.Count > 0
                    ? TimeSpan.FromMilliseconds(stopwatch.Elapsed.TotalMilliseconds / processed * (files.Count - processed))
                    : null,
            });
        }

        return results;
    }

    private async Task<List<FieldMapping>> AutoDetectMappingsAsync(string filePath, CancellationToken cancellationToken)
    {
        var sheets = await _excelReader.ReadSheetsAsync(filePath, cancellationToken).ConfigureAwait(false);
        if (sheets.Count == 0)
        {
            return new List<FieldMapping>();
        }

        string sheetName = sheets[0].Name;
        var preview = await _excelReader.ReadPreviewAsync(filePath, sheetName, AppDefaults.TypeDetectionSampleRows, cancellationToken).ConfigureAwait(false);
        var settings = await _settingsService.LoadWithDefaultsAsync(cancellationToken).ConfigureAwait(false);

        return _fieldDetector.Detect(preview.Columns, preview.Rows, settings.TextKeywords).ToList();
    }

    private async Task<string> GetFirstSheetAsync(string filePath, CancellationToken cancellationToken)
    {
        var sheets = await _excelReader.ReadSheetsAsync(filePath, cancellationToken).ConfigureAwait(false);
        return sheets.Count > 0 ? sheets[0].Name : string.Empty;
    }

    private async Task RecordHistoryAsync(
        ConversionType type,
        string sourcePath,
        string? targetPath,
        ConversionResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            await _history.AddAsync(new ConversionHistoryEntry
            {
                ConversionType = type,
                SourceFileName = Path.GetFileName(sourcePath),
                SourceFilePath = sourcePath,
                TargetFileName = targetPath is null ? string.Empty : Path.GetFileName(targetPath),
                TargetFilePath = targetPath ?? string.Empty,
                Status = result.Status,
                TotalRows = result.TotalRows,
                SuccessRows = result.SuccessRows,
                FailedRows = result.FailedRows,
                ElapsedMilliseconds = (long)result.Elapsed.TotalMilliseconds,
                ErrorMessage = result.Message,
                CreatedTime = DateTime.Now,
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // 历史记录失败不应影响转换结果。
        }
    }

    private static List<string> CollectFiles(BatchConversionRequest request)
    {
        var files = new List<string>(request.SourceFiles);

        if (!string.IsNullOrEmpty(request.SourceDirectory) && Directory.Exists(request.SourceDirectory))
        {
            var option = request.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            IEnumerable<string> discovered;
            if (request.ConversionType == ConversionType.ExcelToDbf)
            {
                discovered = Directory.EnumerateFiles(request.SourceDirectory, "*.xlsx", option)
                    .Concat(Directory.EnumerateFiles(request.SourceDirectory, "*.xls", option));
            }
            else
            {
                discovered = Directory.EnumerateFiles(request.SourceDirectory, "*.dbf", option);
            }

            files.AddRange(discovered);
        }

        return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string BuildTargetPath(string sourceFile, BatchConversionRequest request)
    {
        string outputDir = request.OutputDirectory;
        string targetExtension = request.ConversionType == ConversionType.ExcelToDbf
            ? ".dbf"
            : request.ExcelFormat.FileExtension();

        string baseName = Path.GetFileNameWithoutExtension(sourceFile);
        Directory.CreateDirectory(outputDir);
        string targetPath = Path.Combine(outputDir, baseName + targetExtension);

        if (File.Exists(targetPath) && request.OverwriteMode == OverwriteMode.Rename)
        {
            int suffix = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(outputDir, $"{baseName}_{suffix}{targetExtension}");
                suffix++;
            } while (File.Exists(candidate));
            return candidate;
        }

        return targetPath;
    }
}
