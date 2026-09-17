using System.Diagnostics;
using System.Runtime.CompilerServices;
using ExcelDbfConverter.Core.DTOs;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Application.Converters;

/// <summary>DBF → Excel 转换任务。</summary>
public sealed class DbfToExcelConversionTask : IConversionTask<DbfToExcelRequest>
{
    private readonly IDbfReader _dbfReader;
    private readonly IExcelWriter _excelWriter;

    public DbfToExcelConversionTask(IDbfReader dbfReader, IExcelWriter excelWriter)
    {
        _dbfReader = dbfReader;
        _excelWriter = excelWriter;
    }

    public async Task<ConversionResult> ExecuteAsync(
        DbfToExcelRequest request,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConversionResult();

        string tempPath = request.OutputPath + ".tmp";
        DeleteIfExists(tempPath);

        try
        {
            var structure = await _dbfReader.ReadStructureAsync(request.SourcePath, cancellationToken).ConfigureAwait(false);
            var headers = structure.Schema.Fields.Select(f => f.Name).ToList();
            long totalRows = structure.RecordCount;
            long currentRow = 0;

            var options = new DbfReadOptions
            {
                Encoding = request.Encoding,
                IncludeDeleted = request.IncludeDeleted,
            };

            await _excelWriter.WriteAsync(
                tempPath,
                request.ExcelFormat,
                request.SheetName,
                headers,
                StreamRowsAsync(request, options, totalRows, () => Interlocked.Read(ref currentRow), v => Interlocked.Exchange(ref currentRow, v), progress, stopwatch, cancellationToken),
                request.MaxRowsPerSheet,
                cancellationToken).ConfigureAwait(false);

            File.Move(tempPath, request.OutputPath, overwrite: true);
            result.TotalRows = Interlocked.Read(ref currentRow);
            result.SuccessRows = result.TotalRows;
            result.OutputFilePath = request.OutputPath;
            result.Status = ConversionStatus.Success;
            result.Elapsed = stopwatch.Elapsed;
            result.Message = structure.HasMemoField ? "该 DBF 包含 Memo 字段，第一版本不读取 FPT 文件，相关字段可能为空。" : null;
            return result;
        }
        catch (OperationCanceledException)
        {
            DeleteIfExists(tempPath);
            result.Status = ConversionStatus.Cancelled;
            result.Elapsed = stopwatch.Elapsed;
            return result;
        }
        catch (Exception ex)
        {
            DeleteIfExists(tempPath);
            result.Status = ConversionStatus.Failed;
            result.Message = ex.Message;
            result.Elapsed = stopwatch.Elapsed;
            return result;
        }
    }

    private async IAsyncEnumerable<ExcelRow> StreamRowsAsync(
        DbfToExcelRequest request,
        DbfReadOptions options,
        long totalRows,
        Func<long> getCurrentRow,
        Action<long> setCurrentRow,
        IProgress<ConversionProgress>? progress,
        Stopwatch stopwatch,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var record in _dbfReader.ReadRecordsAsync(request.SourcePath, options, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            long current = getCurrentRow() + 1;
            setCurrentRow(current);

            yield return new ExcelRow
            {
                RowNumber = (int)Math.Min(current, int.MaxValue),
                Values = record.Values,
            };

            if (current % 500 == 0)
            {
                ReportProgress(progress, request.SourcePath, current, totalRows, stopwatch.Elapsed);
            }
        }
    }

    private static void ReportProgress(IProgress<ConversionProgress>? progress, string fileName, long currentRow, long totalRows, TimeSpan elapsed)
    {
        if (progress is null)
        {
            return;
        }
        double percentage = totalRows > 0 ? Math.Min(100, (double)currentRow / totalRows * 100) : 0;
        TimeSpan? remaining = currentRow > 0 && totalRows > 0
            ? TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / currentRow * (totalRows - currentRow))
            : null;
        progress.Report(new ConversionProgress
        {
            CurrentFileName = fileName,
            CurrentRow = currentRow,
            TotalRows = totalRows,
            Percentage = percentage,
            Elapsed = elapsed,
            EstimatedRemaining = remaining,
        });
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // 忽略删除失败。
        }
    }
}
