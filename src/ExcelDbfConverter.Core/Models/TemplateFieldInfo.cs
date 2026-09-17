using ExcelDbfConverter.Core.Enums;

namespace ExcelDbfConverter.Core.Models;

/// <summary>模板字段。</summary>
public sealed class TemplateFieldInfo
{
    public int Id { get; set; }

    public int TemplateId { get; set; }

    public string ExcelColumnName { get; set; } = string.Empty;

    public string DbfFieldName { get; set; } = string.Empty;

    public DbfFieldType DbfFieldType { get; set; } = DbfFieldType.Character;

    public int FieldLength { get; set; }

    public int DecimalCount { get; set; }

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }
}
