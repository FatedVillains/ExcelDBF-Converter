using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>模板持久化仓储。</summary>
public interface ITemplateRepository
{
    Task<IReadOnlyList<TemplateInfo>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TemplateInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TemplateInfo?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<int> SaveAsync(TemplateInfo template, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
