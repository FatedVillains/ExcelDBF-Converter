namespace ExcelDbfConverter.Core.DTOs;

/// <summary>单条转换错误记录。</summary>
public sealed class ConversionError
{
    /// <summary>源数据行号（1 起始）。</summary>
    public long RowNumber { get; set; }

    /// <summary>字段名称。</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>原始数据（文本表示）。</summary>
    public string RawValue { get; set; } = string.Empty;

    /// <summary>错误原因。</summary>
    public string Reason { get; set; } = string.Empty;
}
