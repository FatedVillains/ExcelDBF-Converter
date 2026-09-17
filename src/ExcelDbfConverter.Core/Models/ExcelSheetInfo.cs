namespace ExcelDbfConverter.Core.Models;

/// <summary>Excel Sheet 信息。</summary>
public sealed class ExcelSheetInfo
{
    public string Name { get; set; } = string.Empty;

    /// <summary>数据行数（不含表头）。</summary>
    public int RowCount { get; set; }

    /// <summary>列数。</summary>
    public int ColumnCount { get; set; }
}
