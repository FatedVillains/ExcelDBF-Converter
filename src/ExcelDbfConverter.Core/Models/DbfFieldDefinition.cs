namespace ExcelDbfConverter.Core.Models;

/// <summary>DBF 字段定义。</summary>
public sealed class DbfFieldDefinition
{
    /// <summary>字段名（最多 10 个字符）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>字段类型。</summary>
    public Enums.DbfFieldType Type { get; set; } = Enums.DbfFieldType.Character;

    /// <summary>字段长度（字节）。</summary>
    public int Length { get; set; }

    /// <summary>小数位数（仅 N/F 有效）。</summary>
    public int DecimalCount { get; set; }

    /// <summary>该字段在记录中的字节偏移（含删除标记 1 字节）。由写入器计算。</summary>
    public int OffsetInRecord { get; set; }
}
