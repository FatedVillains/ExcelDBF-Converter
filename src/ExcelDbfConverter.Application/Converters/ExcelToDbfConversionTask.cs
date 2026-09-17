using System.Diagnostics;
using ExcelDbfConverter.Core.DTOs;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Application.Converters;

/// <summary>Excel → DBF 转换任务。</summary>
public sealed class ExcelToDbfConversionTask : IConversionTask<ExcelToDbfRequest>
{
    private readonly IExcelReader _excelReader;
    private readonly IDataValidator _validator;
    private readonly IExcelWriter _excelWriter;
    private readonly Func<IDbfWriter> _writerFactory;

    public ExcelToDbfConversionTask(IExcelReader excelReader, IDataValidator validator, IExcelWriter excelWriter, Func<IDbfWriter> writerFactory)
    {
        _excelReader = excelReader;
        _validator = validator;
        _excelWriter = excelWriter;
        _writerFactory = writerFactory;
    }

    public async Task<ConversionResult> ExecuteAsync(
        ExcelToDbfRequest request,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConversionResult();
        var errors = new List<ConversionError>();

        string tempPath = request.OutputPath + ".tmp";
        DeleteIfExists(tempPath);

        try
        {
            var sheets = await _excelReader.ReadSheetsAsync(request.SourcePath, cancellationToken).ConfigureAwait(false);
            var sheet = sheets.FirstOrDefault(s => s.Name == request.SheetName)
                ?? throw new InvalidDataException($"未找到 Sheet：{request.SheetName}");
            long totalRows = Math.Max(0, sheet.RowCount - (request.HasHeaderRow ? 1 : 0));

            var exportedFields = request.FieldMappings.Where(m => m.IsExport).ToList();
            if (exportedFields.Count == 0)
            {
                throw new InvalidDataException("没有配置任何导出字段。");
            }

            var schema = new DbfTableSchema
            {
                Fields = exportedFields.Select(m => new DbfFieldDefinition
                {
                    Name = m.DbfFieldName,
                    Type = m.DbfFieldType,
                    Length = m.Length,
                    DecimalCount = m.DecimalCount,
                }).ToList(),
            };

            var writer = _writerFactory();
            long currentRow = 0;

            try
            {
                await writer.CreateAsync(tempPath, schema, request.Encoding, cancellationToken).ConfigureAwait(false);

                bool firstRow = true;
                await foreach (var row in _excelReader.ReadRowsAsync(request.SourcePath, request.SheetName, cancellationToken).ConfigureAwait(false))
                {
                    if (request.HasHeaderRow && firstRow)
                    {
                        firstRow = false;
                        continue;
                    }
                    firstRow = false;

                    cancellationToken.ThrowIfCancellationRequested();
                    currentRow++;

                    var record = new DbfRecord();
                    bool skipRow = false;

                    foreach (var field in exportedFields)
                    {
                        object? rawValue = field.ExcelColumnIndex >= 0 && field.ExcelColumnIndex < row.Values.Count
                            ? row.Values[field.ExcelColumnIndex]
                            : null;

                        if (!_validator.TryConvert(rawValue, field, out var converted, out var error))
                        {
                            var conversionError = new ConversionError
                            {
                                RowNumber = row.RowNumber,
                                FieldName = field.DbfFieldName,
                                RawValue = rawValue?.ToString() ?? string.Empty,
                                Reason = error ?? "数据校验失败。",
                            };
                            errors.Add(conversionError);

                            switch (request.ErrorHandling)
                            {
                                case ErrorHandlingMode.Stop:
                                    result.Status = ConversionStatus.Failed;
                                    result.Message = $"第 {row.RowNumber} 行字段「{field.DbfFieldName}」：{error}";
                                    writer.Dispose();
                                    DeleteIfExists(tempPath);
                                    return result;
                                case ErrorHandlingMode.SkipRow:
                                    skipRow = true;
                                    break;
                                case ErrorHandlingMode.SetNull:
                                    converted = null;
                                    break;
                            }
                        }

                        if (skipRow)
                        {
                            break;
                        }

                        record.Values.Add(converted);
                    }

                    if (skipRow)
                    {
                        result.FailedRows++;
                        continue;
                    }

                    await writer.WriteRecordAsync(record, cancellationToken).ConfigureAwait(false);
                    result.SuccessRows++;

                    if (currentRow % 500 == 0)
                    {
                        ReportProgress(progress, request.SourcePath, currentRow, totalRows, stopwatch.Elapsed);
                    }
                }

                await writer.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                writer.Dispose();
            }

            File.Move(tempPath, request.OutputPath, overwrite: true);
            result.TotalRows = currentRow;
            result.OutputFilePath = request.OutputPath;
            result.Status = errors.Count > 0 ? ConversionStatus.Partial : ConversionStatus.Success;
            result.Errors = errors;
            result.Elapsed = stopwatch.Elapsed;

            if (errors.Count > 0)
            {
                result.ErrorReportPath = await WriteErrorReportAsync(request, errors, cancellationToken).ConfigureAwait(false);
            }

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

    private async Task<string?> WriteErrorReportAsync(ExcelToDbfRequest request, List<ConversionError> errors, CancellationToken cancellationToken)
    {
        try
        {
            string reportPath = Path.Combine(
                Path.GetDirectoryName(request.OutputPath) ?? string.Empty,
                $"转换错误报告_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            var rows = errors.Select(e => new ExcelRow
            {
                RowNumber = (int)Math.Min(e.RowNumber, int.MaxValue),
                Values = new List<object?> { e.RowNumber, e.FieldName, e.RawValue, e.Reason },
            });

            await _excelWriter.WriteAsync(
                reportPath,
                ExcelFormat.Xlsx,
                "错误报告",
                new[] { "行号", "字段名称", "原始数据", "错误原因" },
                ToAsyncEnumerable(rows),
                1_000_000,
                cancellationToken).ConfigureAwait(false);

            return reportPath;
        }
        catch
        {
            return null;
        }
    }

    private static async IAsyncEnumerable<ExcelRow> ToAsyncEnumerable(IEnumerable<ExcelRow> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }
        await Task.CompletedTask;
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
