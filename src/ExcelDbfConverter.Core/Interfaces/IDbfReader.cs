using ExcelDbfConverter.Core.DTOs;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>DBF 读取器接口。</summary>
public interface IDbfReader
{
    /// <summary>读取 DBF 表结构。</summary>
    Task<DbfTableInfo> ReadStructureAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>流式读取记录。</summary>
    IAsyncEnumerable<DbfRecord> ReadRecordsAsync(
        string filePath,
        DbfReadOptions options,
        CancellationToken cancellationToken = default);
}
