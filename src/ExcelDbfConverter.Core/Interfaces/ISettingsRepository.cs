using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>应用设置持久化仓储。</summary>
public interface ISettingsRepository
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
