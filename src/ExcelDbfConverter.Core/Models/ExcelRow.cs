namespace ExcelDbfConverter.Core.Models;

/// <summary>Excel 一行数据。</summary>
public sealed class ExcelRow
{
    /// <summary>原始行号（1 起始，含表头）。</summary>
    public int RowNumber { get; set; }

    /// <summary>单元格值（按列顺序）。</summary>
    public List<object?> Values { get; set; } = new();
}
