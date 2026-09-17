namespace ExcelDbfConverter.Core.Models;

/// <summary>Excel 预览结果。</summary>
public sealed class ExcelPreviewResult
{
    /// <summary>列信息。</summary>
    public List<ExcelColumnInfo> Columns { get; set; } = new();

    /// <summary>预览行。</summary>
    public List<ExcelRow> Rows { get; set; } = new();

    /// <summary>总数据行数（不含表头）。</summary>
    public long TotalRows { get; set; }
}
