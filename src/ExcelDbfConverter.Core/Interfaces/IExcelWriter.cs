using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>Excel 写入器接口（DBF → Excel）。</summary>
public interface IExcelWriter
{
    /// <summary>
    /// 将表头与数据行流式写入 Excel，超过 <paramref name="maxRowsPerSheet"/> 时自动拆分 Sheet。
    /// </summary>
    Task WriteAsync(
        string filePath,
        ExcelFormat format,
        string sheetName,
        IReadOnlyList<string> headers,
        IAsyncEnumerable<ExcelRow> rows,
        int maxRowsPerSheet,
        CancellationToken cancellationToken = default);
}
