using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>Excel 读取器接口。</summary>
public interface IExcelReader
{
    /// <summary>读取工作簿中的所有 Sheet 信息（名称、行数、列数）。</summary>
    Task<IReadOnlyList<ExcelSheetInfo>> ReadSheetsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>读取表头与预览数据。</summary>
    Task<ExcelPreviewResult> ReadPreviewAsync(
        string filePath,
        string sheetName,
        int maxRows,
        CancellationToken cancellationToken = default);

    /// <summary>流式读取 Sheet 的所有数据行（含表头，逐行返回）。</summary>
    IAsyncEnumerable<ExcelRow> ReadRowsAsync(
        string filePath,
        string sheetName,
        CancellationToken cancellationToken = default);
}
