namespace ExcelDbfConverter.Core.Models;

/// <summary>DBF 单条记录。</summary>
public sealed class DbfRecord
{
    /// <summary>是否已删除。</summary>
    public bool IsDeleted { get; set; }

    /// <summary>字段值（按字段顺序）。</summary>
    public List<object?> Values { get; set; } = new();
}
