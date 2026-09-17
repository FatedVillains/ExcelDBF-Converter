using System.Runtime.CompilerServices;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using NPOI.SS.UserModel;

namespace ExcelDbfConverter.Infrastructure.Excel;

/// <summary>基于 NPOI 的 Excel 读取器（支持 .xls / .xlsx）。</summary>
public sealed class NpoiExcelReader : IExcelReader
{
    public async Task<IReadOnlyList<ExcelSheetInfo>> ReadSheetsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            IWorkbook workbook = OpenWorkbook(filePath);
            try
            {
                var result = new List<ExcelSheetInfo>();
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var sheet = workbook.GetSheetAt(i);
                result.Add(new ExcelSheetInfo
                {
                    Name = sheet.SheetName,
                    RowCount = Math.Max(0, sheet.LastRowNum + 1),
                    ColumnCount = GetColumnCount(sheet),
                });
                }
                return (IReadOnlyList<ExcelSheetInfo>)result;
            }
            finally
            {
                workbook.Close();
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExcelPreviewResult> ReadPreviewAsync(
        string filePath,
        string sheetName,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            IWorkbook workbook = OpenWorkbook(filePath);
            try
            {
                var sheet = workbook.GetSheet(sheetName)
                    ?? throw new InvalidDataException($"未找到 Sheet：{sheetName}");

                int columnCount = GetColumnCount(sheet);
                var columns = new List<ExcelColumnInfo>();
                var headerRow = sheet.GetRow(0);
                for (int c = 0; c < columnCount; c++)
                {
                    columns.Add(new ExcelColumnInfo
                    {
                        Index = c,
                        Name = GetHeaderName(headerRow?.GetCell(c), c),
                    });
                }

                var rows = new List<ExcelRow>();
                int lastRow = sheet.LastRowNum;
                for (int r = 1; r <= lastRow && rows.Count < maxRows; r++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var row = sheet.GetRow(r);
                    if (row is null)
                    {
                        continue;
                    }
                    rows.Add(ReadRow(row, columnCount, r + 1));
                }

                return new ExcelPreviewResult
                {
                    Columns = columns,
                    Rows = rows,
                    TotalRows = Math.Max(0, lastRow + 1),
                };
            }
            finally
            {
                workbook.Close();
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<ExcelRow> ReadRowsAsync(
        string filePath,
        string sheetName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IWorkbook workbook = OpenWorkbook(filePath);
        try
        {
            var sheet = workbook.GetSheet(sheetName)
                ?? throw new InvalidDataException($"未找到 Sheet：{sheetName}");

            int columnCount = GetColumnCount(sheet);
            int lastRow = sheet.LastRowNum;

            for (int r = 0; r <= lastRow; r++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = sheet.GetRow(r);
                var values = new List<object?>(columnCount);
                for (int c = 0; c < columnCount; c++)
                {
                    values.Add(GetCellValue(row?.GetCell(c)));
                }
                yield return new ExcelRow { RowNumber = r + 1, Values = values };
                await Task.Yield();
            }
        }
        finally
        {
            workbook.Close();
        }
    }

    private static IWorkbook OpenWorkbook(string filePath)
    {
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        try
        {
            return WorkbookFactory.Create(stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private static int GetColumnCount(ISheet sheet)
    {
        int maxCols = 0;
        for (int r = 0; r <= sheet.LastRowNum; r++)
        {
            var row = sheet.GetRow(r);
            if (row is not null && row.LastCellNum > maxCols)
            {
                maxCols = row.LastCellNum;
            }
        }
        return maxCols;
    }

    private static string GetHeaderName(ICell? cell, int index)
    {
        string name = cell is null ? string.Empty : new DataFormatter().FormatCellValue(cell).Trim();
        return string.IsNullOrWhiteSpace(name) ? $"列{index + 1}" : name;
    }

    private static ExcelRow ReadRow(IRow row, int columnCount, int rowNumber)
    {
        var values = new List<object?>(columnCount);
        for (int c = 0; c < columnCount; c++)
        {
            values.Add(GetCellValue(row.GetCell(c)));
        }
        return new ExcelRow { RowNumber = rowNumber, Values = values };
    }

    private static object? GetCellValue(ICell? cell)
    {
        if (cell is null)
        {
            return null;
        }

        switch (cell.CellType)
        {
            case CellType.Blank:
                return null;
            case CellType.Boolean:
                return cell.BooleanCellValue;
            case CellType.Numeric:
                return DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue : cell.NumericCellValue;
            case CellType.String:
                return cell.StringCellValue;
            case CellType.Formula:
                return GetFormulaValue(cell);
            default:
                return null;
        }
    }

    private static object? GetFormulaValue(ICell cell)
    {
        switch (cell.CachedFormulaResultType)
        {
            case CellType.Numeric:
                return cell.NumericCellValue;
            case CellType.String:
                return cell.StringCellValue;
            case CellType.Boolean:
                return cell.BooleanCellValue;
            default:
                return null;
        }
    }
}
