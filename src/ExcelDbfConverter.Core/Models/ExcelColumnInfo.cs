namespace ExcelDbfConverter.Core.Models;

/// <summary>Excel 列信息。</summary>
public sealed class ExcelColumnInfo
{
    /// <summary>列索引（从 0 开始）。</summary>
    public int Index { get; set; }

    /// <summary>表头名称。</summary>
    public string Name { get; set; } = string.Empty;
}
