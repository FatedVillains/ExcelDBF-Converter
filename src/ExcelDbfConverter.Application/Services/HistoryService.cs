using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Application.Services;

/// <summary>转换历史服务。</summary>
public sealed class HistoryService : IHistoryService
{
    private readonly IHistoryRepository _repository;

    public HistoryService(IHistoryRepository repository)
    {
        _repository = repository;
    }

    public Task AddAsync(ConversionHistoryEntry entry, CancellationToken cancellationToken = default) =>
        _repository.AddAsync(entry, cancellationToken);

    public Task<IReadOnlyList<ConversionHistoryEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        _repository.GetRecentAsync(count, cancellationToken);

    public Task<IReadOnlyList<ConversionHistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        _repository.ClearAsync(cancellationToken);
}
