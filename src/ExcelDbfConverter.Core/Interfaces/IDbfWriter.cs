using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>DBF 写入器接口。</summary>
public interface IDbfWriter : IDisposable
{
    /// <summary>创建 DBF 文件并写入表头（记录数为 0）。</summary>
    Task CreateAsync(
        string filePath,
        DbfTableSchema schema,
        DbfEncodingKind encoding,
        CancellationToken cancellationToken = default);

    /// <summary>写入一条记录。</summary>
    Task WriteRecordAsync(DbfRecord record, CancellationToken cancellationToken = default);

    /// <summary>完成写入：回填记录数并写入文件结束标记。</summary>
    Task CompleteAsync(CancellationToken cancellationToken = default);
}
