using System.Globalization;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Infrastructure.SQLite;

namespace ExcelDbfConverter.Infrastructure.SQLite;

/// <summary>基于 SQLite 的转换历史仓储。</summary>
public sealed class SqliteHistoryRepository : IHistoryRepository
{
    private readonly DatabaseInitializer _db;

    public SqliteHistoryRepository(DatabaseInitializer db)
    {
        _db = db;
    }

    public async Task AddAsync(ConversionHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ConversionHistory
                (ConversionType, SourceFileName, SourceFilePath, TargetFileName, TargetFilePath,
                 Status, TotalRows, SuccessRows, FailedRows, ElapsedMilliseconds, ErrorMessage, CreatedTime)
            VALUES
                ($type, $sfn, $sfp, $tfn, $tfp, $status, $total, $success, $failed, $elapsed, $err, $created);
            """;
        command.Parameters.AddWithValue("$type", (int)entry.ConversionType);
        command.Parameters.AddWithValue("$sfn", entry.SourceFileName);
        command.Parameters.AddWithValue("$sfp", entry.SourceFilePath);
        command.Parameters.AddWithValue("$tfn", entry.TargetFileName);
        command.Parameters.AddWithValue("$tfp", entry.TargetFilePath);
        command.Parameters.AddWithValue("$status", (int)entry.Status);
        command.Parameters.AddWithValue("$total", entry.TotalRows);
        command.Parameters.AddWithValue("$success", entry.SuccessRows);
        command.Parameters.AddWithValue("$failed", entry.FailedRows);
        command.Parameters.AddWithValue("$elapsed", entry.ElapsedMilliseconds);
        command.Parameters.AddWithValue("$err", (object?)entry.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("$created", entry.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ConversionHistoryEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM ConversionHistory ORDER BY Id DESC LIMIT {count};";
        return await ReadEntriesAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ConversionHistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM ConversionHistory ORDER BY Id DESC;";
        return await ReadEntriesAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ConversionHistory WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ConversionHistory;";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<ConversionHistoryEntry>> ReadEntriesAsync(Microsoft.Data.Sqlite.SqliteCommand command, CancellationToken cancellationToken)
    {
        var entries = new List<ConversionHistoryEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(new ConversionHistoryEntry
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ConversionType = (ConversionType)reader.GetInt32(reader.GetOrdinal("ConversionType")),
                SourceFileName = reader.GetString(reader.GetOrdinal("SourceFileName")),
                SourceFilePath = reader.GetString(reader.GetOrdinal("SourceFilePath")),
                TargetFileName = reader.GetString(reader.GetOrdinal("TargetFileName")),
                TargetFilePath = reader.GetString(reader.GetOrdinal("TargetFilePath")),
                Status = (ConversionStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                TotalRows = reader.GetInt64(reader.GetOrdinal("TotalRows")),
                SuccessRows = reader.GetInt64(reader.GetOrdinal("SuccessRows")),
                FailedRows = reader.GetInt64(reader.GetOrdinal("FailedRows")),
                ElapsedMilliseconds = reader.GetInt64(reader.GetOrdinal("ElapsedMilliseconds")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage")),
                CreatedTime = ParseDate(reader.GetString(reader.GetOrdinal("CreatedTime"))),
            });
        }
        return entries;
    }

    private static DateTime ParseDate(string value) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? result
            : DateTime.Now;
}
