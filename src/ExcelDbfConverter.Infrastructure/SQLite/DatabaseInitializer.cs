using Microsoft.Data.Sqlite;

namespace ExcelDbfConverter.Infrastructure.SQLite;

/// <summary>SQLite 数据库初始化与连接工厂。</summary>
public sealed class DatabaseInitializer
{
    private readonly string _databasePath;

    public DatabaseInitializer(string databasePath)
    {
        _databasePath = databasePath;
    }

    public string ConnectionString => $"Data Source={_databasePath}";

    /// <summary>创建目录并建表。</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = Open();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Template (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL DEFAULT '',
                Version TEXT NOT NULL DEFAULT '1.0',
                CreatedTime TEXT NOT NULL,
                UpdatedTime TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS TemplateField (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TemplateId INTEGER NOT NULL,
                ExcelColumnName TEXT NOT NULL DEFAULT '',
                DbfFieldName TEXT NOT NULL DEFAULT '',
                DbfFieldType TEXT NOT NULL DEFAULT 'C',
                FieldLength INTEGER NOT NULL DEFAULT 10,
                DecimalCount INTEGER NOT NULL DEFAULT 0,
                IsRequired INTEGER NOT NULL DEFAULT 0,
                SortOrder INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS ConversionHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ConversionType INTEGER NOT NULL,
                SourceFileName TEXT NOT NULL DEFAULT '',
                SourceFilePath TEXT NOT NULL DEFAULT '',
                TargetFileName TEXT NOT NULL DEFAULT '',
                TargetFilePath TEXT NOT NULL DEFAULT '',
                Status INTEGER NOT NULL,
                TotalRows INTEGER NOT NULL DEFAULT 0,
                SuccessRows INTEGER NOT NULL DEFAULT 0,
                FailedRows INTEGER NOT NULL DEFAULT 0,
                ElapsedMilliseconds INTEGER NOT NULL DEFAULT 0,
                ErrorMessage TEXT NULL,
                CreatedTime TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS AppSetting (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SettingKey TEXT NOT NULL UNIQUE,
                SettingValue TEXT NOT NULL,
                UpdatedTime TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public SqliteConnection Open() => new(ConnectionString);
}
