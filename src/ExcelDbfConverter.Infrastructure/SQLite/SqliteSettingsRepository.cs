using System.Text.Json;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Infrastructure.SQLite;
using Microsoft.Data.Sqlite;

namespace ExcelDbfConverter.Infrastructure.SQLite;

/// <summary>基于 SQLite 的应用设置仓储。</summary>
public sealed class SqliteSettingsRepository : ISettingsRepository
{
    private readonly DatabaseInitializer _db;

    public SqliteSettingsRepository(DatabaseInitializer db)
    {
        _db = db;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SettingKey, SettingValue FROM AppSetting;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            values[reader.GetString(0)] = reader.GetString(1);
        }

        var settings = new AppSettings();
        if (values.TryGetValue("DefaultOutputDirectory", out var dir))
        {
            settings.DefaultOutputDirectory = dir;
        }
        if (values.TryGetValue("DefaultExcelFormat", out var fmt) && int.TryParse(fmt, out var fmtInt))
        {
            settings.DefaultExcelFormat = (ExcelFormat)fmtInt;
        }
        if (values.TryGetValue("DefaultDbfEncoding", out var enc) && int.TryParse(enc, out var encInt))
        {
            settings.DefaultDbfEncoding = (DbfEncodingKind)encInt;
        }
        if (values.TryGetValue("PreviewRowCount", out var prev) && int.TryParse(prev, out var prevInt))
        {
            settings.PreviewRowCount = prevInt;
        }
        if (values.TryGetValue("TextKeywords", out var keywords))
        {
            settings.TextKeywords = JsonSerializer.Deserialize<List<string>>(keywords) ?? new List<string>();
        }

        return settings;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

        await UpsertAsync(connection, transaction, "DefaultOutputDirectory", settings.DefaultOutputDirectory, now, cancellationToken).ConfigureAwait(false);
        await UpsertAsync(connection, transaction, "DefaultExcelFormat", ((int)settings.DefaultExcelFormat).ToString(), now, cancellationToken).ConfigureAwait(false);
        await UpsertAsync(connection, transaction, "DefaultDbfEncoding", ((int)settings.DefaultDbfEncoding).ToString(), now, cancellationToken).ConfigureAwait(false);
        await UpsertAsync(connection, transaction, "PreviewRowCount", settings.PreviewRowCount.ToString(), now, cancellationToken).ConfigureAwait(false);
        await UpsertAsync(connection, transaction, "TextKeywords", JsonSerializer.Serialize(settings.TextKeywords), now, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpsertAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        string key,
        string value,
        string updatedTime,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = """
            INSERT INTO AppSetting (SettingKey, SettingValue, UpdatedTime)
            VALUES ($key, $value, $time)
            ON CONFLICT(SettingKey) DO UPDATE SET SettingValue = $value, UpdatedTime = $time;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.Parameters.AddWithValue("$time", updatedTime);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
