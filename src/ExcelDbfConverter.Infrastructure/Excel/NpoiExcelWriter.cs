using ExcelDbfConverter.Core.Constants;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming;

namespace ExcelDbfConverter.Infrastructure.Excel;

/// <summary>基于 NPOI 的 Excel 写入器（DBF → Excel，支持大文件流式写入与自动拆分 Sheet）。</summary>
public sealed class NpoiExcelWriter : IExcelWriter
{
    public async Task WriteAsync(
        string filePath,
        ExcelFormat format,
        string sheetName,
        IReadOnlyList<string> headers,
        IAsyncEnumerable<ExcelRow> rows,
        int maxRowsPerSheet,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            int hardLimit = format == ExcelFormat.Xls ? ExcelConstants.XlsMaxRows : ExcelConstants.XlsxMaxRows;
            int rowsPerSheet = Math.Clamp(maxRowsPerSheet, 1, hardLimit - 1);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            IWorkbook workbook = format == ExcelFormat.Xls
                ? new HSSFWorkbook()
                : new SXSSFWorkbook(100);

            try
            {
                var dateStyle = CreateDateStyle(workbook, "yyyy-MM-dd");
                var dateTimeStyle = CreateDateStyle(workbook, "yyyy-MM-dd HH:mm:ss");

                ISheet? sheet = null;
                int sheetIndex = 0;
                int rowInSheet = 0;

                void EnsureSheet()
                {
                    if (sheet is null || rowInSheet >= rowsPerSheet)
                    {
                        string name = sheetIndex == 0 ? sheetName : $"{sheetName}_{sheetIndex + 1}";
                        sheet = workbook.CreateSheet(SanitizeSheetName(name));
                        WriteHeader(sheet, headers);
                        sheetIndex++;
                        rowInSheet = 0;
                    }
                }

                await foreach (var row in rows.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    EnsureSheet();
                    WriteRow(sheet!, row, headers.Count, dateStyle, dateTimeStyle);
                    rowInSheet++;
                }

                // 空数据也至少创建一个带表头的 Sheet。
                if (sheet is null)
                {
                    EnsureSheet();
                }

                await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    workbook.Write(stream);
                }
            }
            finally
            {
                workbook.Close();
                (workbook as IDisposable)?.Dispose();
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private static ICellStyle CreateDateStyle(IWorkbook workbook, string format)
    {
        var style = workbook.CreateCellStyle();
        style.DataFormat = workbook.CreateDataFormat().GetFormat(format);
        return style;
    }

    private static void WriteHeader(ISheet sheet, IReadOnlyList<string> headers)
    {
        var row = sheet.CreateRow(0);
        for (int c = 0; c < headers.Count; c++)
        {
            row.CreateCell(c).SetCellValue(headers[c] ?? string.Empty);
        }
    }

    private static void WriteRow(ISheet sheet, ExcelRow row, int columnCount, ICellStyle dateStyle, ICellStyle dateTimeStyle)
    {
        var dataRow = sheet.CreateRow(sheet.LastRowNum + 1);
        for (int c = 0; c < columnCount; c++)
        {
            object? value = c < row.Values.Count ? row.Values[c] : null;
            WriteCell(dataRow.CreateCell(c), value, dateStyle, dateTimeStyle);
        }
    }

    private static void WriteCell(ICell cell, object? value, ICellStyle dateStyle, ICellStyle dateTimeStyle)
    {
        switch (value)
        {
            case null:
                cell.SetCellValue(string.Empty);
                break;
            case string s:
                cell.SetCellValue(s);
                break;
            case bool b:
                cell.SetCellValue(b);
                break;
            case DateTime dt:
                cell.SetCellValue(dt);
                cell.CellStyle = dt.TimeOfDay == TimeSpan.Zero ? dateStyle : dateTimeStyle;
                break;
            case decimal dec:
                cell.SetCellValue(Convert.ToDouble(dec));
                break;
            case int i:
                cell.SetCellValue((double)i);
                break;
            case long l:
                cell.SetCellValue((double)l);
                break;
            case double d:
                cell.SetCellValue(d);
                break;
            case float f:
                cell.SetCellValue((double)f);
                break;
            default:
                cell.SetCellValue(value.ToString() ?? string.Empty);
                break;
        }
    }

    private static string SanitizeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = ExcelConstants.DefaultSheetName;
        }

        foreach (char c in ExcelConstants.InvalidSheetNameChars)
        {
            name = name.Replace(c.ToString(), "_");
        }

        if (name.Length > ExcelConstants.MaxSheetNameLength)
        {
            name = name[..ExcelConstants.MaxSheetNameLength];
        }

        return name;
    }
}
