using ExcelDbfConverter.Core.Enums;

namespace ExcelDbfConverter.Core.Models;

/// <summary>Excel 列到 DBF 字段的映射配置。</summary>
public sealed class FieldMapping
{
    /// <summary>Excel 列名。</summary>
    public string ExcelColumnName { get; set; } = string.Empty;

    /// <summary>Excel 列索引（-1 表示无对应列，用于模板新增字段）。</summary>
    public int ExcelColumnIndex { get; set; } = -1;

    /// <summary>DBF 字段名。</summary>
    public string DbfFieldName { get; set; } = string.Empty;

    /// <summary>DBF 字段类型。</summary>
    public DbfFieldType DbfFieldType { get; set; } = DbfFieldType.Character;

    /// <summary>字段长度。</summary>
    public int Length { get; set; }

    /// <summary>小数位数。</summary>
    public int DecimalCount { get; set; }

    /// <summary>是否导出。</summary>
    public bool IsExport { get; set; } = true;
}
