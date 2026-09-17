using ExcelDbfConverter.Core.Constants;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Application.Services;

/// <summary>系统设置服务。</summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repository;

    public SettingsService(ISettingsRepository repository)
    {
        _repository = repository;
    }

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) =>
        _repository.LoadAsync(cancellationToken);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default) =>
        _repository.SaveAsync(settings, cancellationToken);

    public async Task<AppSettings> LoadWithDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);

        if (settings.PreviewRowCount <= 0)
        {
            settings.PreviewRowCount = AppDefaults.DefaultPreviewRows;
        }
        if (settings.TextKeywords is null || settings.TextKeywords.Count == 0)
        {
            settings.TextKeywords = new List<string>(AppDefaults.DefaultTextKeywords);
        }

        return settings;
    }

    public static AppSettings CreateDefault() => new()
    {
        DefaultExcelFormat = ExcelFormat.Xlsx,
        DefaultDbfEncoding = DbfEncodingKind.Auto,
        PreviewRowCount = AppDefaults.DefaultPreviewRows,
        TextKeywords = new List<string>(AppDefaults.DefaultTextKeywords),
    };
}
