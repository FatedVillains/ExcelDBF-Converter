namespace ExcelDbfConverter.Core.Models;

/// <summary>DBF 表结构。</summary>
public sealed class DbfTableSchema
{
    /// <summary>字段集合。</summary>
    public List<DbfFieldDefinition> Fields { get; set; } = new();

    /// <summary>计算记录长度（含 1 字节删除标记）。</summary>
    public int RecordLength => 1 + Fields.Sum(f => f.Length);

    /// <summary>计算表头长度：前缀 32 + 字段描述符 + 0x0D 终止符 + VFP backlink 263。</summary>
    public int HeaderLength => Constants.DbfConstants.HeaderPrefixSize
        + Fields.Count * Constants.DbfConstants.FieldDescriptorSize
        + Constants.DbfConstants.HeaderTerminatorSize
        + Constants.DbfConstants.HeaderBacklinkSize;
}
