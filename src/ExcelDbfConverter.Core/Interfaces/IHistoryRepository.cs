using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>转换历史持久化仓储。</summary>
public interface IHistoryRepository
{
    Task AddAsync(ConversionHistoryEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversionHistoryEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversionHistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
