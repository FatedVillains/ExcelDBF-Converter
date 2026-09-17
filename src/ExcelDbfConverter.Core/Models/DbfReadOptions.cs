using ExcelDbfConverter.Core.Enums;

namespace ExcelDbfConverter.Core.Models;

/// <summary>DBF 读取选项。</summary>
public sealed class DbfReadOptions
{
    /// <summary>字符编码；Auto 时自动识别。</summary>
    public DbfEncodingKind Encoding { get; set; } = DbfEncodingKind.Auto;

    /// <summary>是否包含已删除记录。</summary>
    public bool IncludeDeleted { get; set; }

    /// <summary>最多读取的记录数；null 表示全部。</summary>
    public int? MaxRecords { get; set; }
}
