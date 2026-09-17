using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>系统设置服务。</summary>
public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);

    /// <summary>加载并回填默认值（首次运行时）。</summary>
    Task<AppSettings> LoadWithDefaultsAsync(CancellationToken cancellationToken = default);
}
