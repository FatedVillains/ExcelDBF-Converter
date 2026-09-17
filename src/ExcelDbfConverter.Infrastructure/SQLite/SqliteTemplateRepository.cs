using System.Globalization;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Infrastructure.SQLite;
using Microsoft.Data.Sqlite;

namespace ExcelDbfConverter.Infrastructure.SQLite;

/// <summary>基于 SQLite 的模板仓储。</summary>
public sealed class SqliteTemplateRepository : ITemplateRepository
{
    private readonly DatabaseInitializer _db;

    public SqliteTemplateRepository(DatabaseInitializer db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TemplateInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var templates = new List<TemplateInfo>();
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Id, Name, Description, Version, CreatedTime, UpdatedTime FROM Template ORDER BY Id DESC;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                templates.Add(new TemplateInfo
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    Version = reader.GetString(3),
                    CreatedTime = ParseDate(reader.GetString(4)),
                    UpdatedTime = ParseDate(reader.GetString(5)),
                });
            }
        }

        foreach (var template in templates)
        {
            template.Fields = await LoadFieldsAsync(connection, template.Id, cancellationToken).ConfigureAwait(false);
        }

        return templates;
    }

    public async Task<TemplateInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Description, Version, CreatedTime, UpdatedTime FROM Template WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var template = new TemplateInfo
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.GetString(2),
            Version = reader.GetString(3),
            CreatedTime = ParseDate(reader.GetString(4)),
            UpdatedTime = ParseDate(reader.GetString(5)),
        };
        await reader.DisposeAsync().ConfigureAwait(false);

        template.Fields = await LoadFieldsAsync(connection, template.Id, cancellationToken).ConfigureAwait(false);
        return template;
    }

    public async Task<TemplateInfo?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM Template WHERE Name = $name LIMIT 1;";
        command.Parameters.AddWithValue("$name", name);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        return await GetByIdAsync(Convert.ToInt32(result, CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> SaveAsync(TemplateInfo template, CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        template.UpdatedTime = DateTime.Now;

        if (template.Id <= 0)
        {
            template.CreatedTime = template.UpdatedTime;
            await using var insert = connection.CreateCommand();
            insert.Transaction = (SqliteTransaction)transaction;
            insert.CommandText = """
                INSERT INTO Template (Name, Description, Version, CreatedTime, UpdatedTime)
                VALUES ($name, $desc, $ver, $created, $updated);
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$name", template.Name);
            insert.Parameters.AddWithValue("$desc", template.Description);
            insert.Parameters.AddWithValue("$ver", template.Version);
            insert.Parameters.AddWithValue("$created", FormatDate(template.CreatedTime));
            insert.Parameters.AddWithValue("$updated", FormatDate(template.UpdatedTime));
            var id = await insert.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            template.Id = Convert.ToInt32(id, CultureInfo.InvariantCulture);
        }
        else
        {
            await using var update = connection.CreateCommand();
            update.Transaction = (SqliteTransaction)transaction;
            update.CommandText = """
                UPDATE Template SET Name = $name, Description = $desc, Version = $ver, UpdatedTime = $updated
                WHERE Id = $id;
                """;
            update.Parameters.AddWithValue("$name", template.Name);
            update.Parameters.AddWithValue("$desc", template.Description);
            update.Parameters.AddWithValue("$ver", template.Version);
            update.Parameters.AddWithValue("$updated", FormatDate(template.UpdatedTime));
            update.Parameters.AddWithValue("$id", template.Id);
            await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await using var deleteFields = connection.CreateCommand();
            deleteFields.Transaction = (SqliteTransaction)transaction;
            deleteFields.CommandText = "DELETE FROM TemplateField WHERE TemplateId = $id;";
            deleteFields.Parameters.AddWithValue("$id", template.Id);
            await deleteFields.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        int order = 1;
        foreach (var field in template.Fields.OrderBy(f => f.SortOrder))
        {
            await using var insertField = connection.CreateCommand();
            insertField.Transaction = (SqliteTransaction)transaction;
            insertField.CommandText = """
                INSERT INTO TemplateField
                    (TemplateId, ExcelColumnName, DbfFieldName, DbfFieldType, FieldLength, DecimalCount, IsRequired, SortOrder)
                VALUES
                    ($tid, $excel, $dbf, $type, $len, $dec, $req, $order);
                """;
            insertField.Parameters.AddWithValue("$tid", template.Id);
            insertField.Parameters.AddWithValue("$excel", field.ExcelColumnName);
            insertField.Parameters.AddWithValue("$dbf", field.DbfFieldName);
            insertField.Parameters.AddWithValue("$type", field.DbfFieldType.ToDbfCode().ToString());
            insertField.Parameters.AddWithValue("$len", field.FieldLength);
            insertField.Parameters.AddWithValue("$dec", field.DecimalCount);
            insertField.Parameters.AddWithValue("$req", field.IsRequired ? 1 : 0);
            insertField.Parameters.AddWithValue("$order", field.SortOrder > 0 ? field.SortOrder : order);
            await insertField.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            order++;
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return template.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var deleteFields = connection.CreateCommand())
        {
            deleteFields.Transaction = (SqliteTransaction)transaction;
            deleteFields.CommandText = "DELETE FROM TemplateField WHERE TemplateId = $id;";
            deleteFields.Parameters.AddWithValue("$id", id);
            await deleteFields.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var deleteTemplate = connection.CreateCommand())
        {
            deleteTemplate.Transaction = (SqliteTransaction)transaction;
            deleteTemplate.CommandText = "DELETE FROM Template WHERE Id = $id;";
            deleteTemplate.Parameters.AddWithValue("$id", id);
            await deleteTemplate.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<TemplateFieldInfo>> LoadFieldsAsync(SqliteConnection connection, int templateId, CancellationToken cancellationToken)
    {
        var fields = new List<TemplateFieldInfo>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, TemplateId, ExcelColumnName, DbfFieldName, DbfFieldType, FieldLength, DecimalCount, IsRequired, SortOrder
            FROM TemplateField WHERE TemplateId = $id ORDER BY SortOrder, Id;
            """;
        command.Parameters.AddWithValue("$id", templateId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            fields.Add(new TemplateFieldInfo
            {
                Id = reader.GetInt32(0),
                TemplateId = reader.GetInt32(1),
                ExcelColumnName = reader.GetString(2),
                DbfFieldName = reader.GetString(3),
                DbfFieldType = DbfFieldTypeExtensions.FromDbfCode(reader.GetString(4)[0]) ?? DbfFieldType.Character,
                FieldLength = reader.GetInt32(5),
                DecimalCount = reader.GetInt32(6),
                IsRequired = reader.GetInt32(7) != 0,
                SortOrder = reader.GetInt32(8),
            });
        }
        return fields;
    }

    private static string FormatDate(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static DateTime ParseDate(string value) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? result
            : DateTime.Now;
}
