using ExcelDbfConverter.Application;
using ExcelDbfConverter.Application.Services;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Infrastructure;
using ExcelDbfConverter.Infrastructure.Configuration;
using ExcelDbfConverter.Infrastructure.SQLite;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelDbfConverter.Integration.Tests;

/// <summary>模板与历史记录集成测试。</summary>
public class TemplateHistoryIntegrationTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly string _testDir;

    public TemplateHistoryIntegrationTests()
    {
        _testDir = TestDataFactory.EnsureTestDir();

        var paths = new AppPaths(_testDir);
        paths.EnsureDirectories();

        var services = new ServiceCollection();
        services.AddInfrastructure(paths);
        services.AddApplication();
        _services = services.BuildServiceProvider();

        _services.GetRequiredService<DatabaseInitializer>().InitializeAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _services.Dispose();
        TestDataFactory.CleanupTestDir(_testDir);
    }

    private TemplateInfo CreateTestTemplate(string name = "测试模板")
    {
        return new TemplateInfo
        {
            Name = name,
            Description = "集成测试模板",
            Version = "1.0",
            Fields = new List<TemplateFieldInfo>
            {
                new() { ExcelColumnName = "姓名", DbfFieldName = "XM", DbfFieldType = DbfFieldType.Character, FieldLength = 20, SortOrder = 1 },
                new() { ExcelColumnName = "身份证号", DbfFieldName = "SFZH", DbfFieldType = DbfFieldType.Character, FieldLength = 18, SortOrder = 2 },
                new() { ExcelColumnName = "年龄", DbfFieldName = "NL", DbfFieldType = DbfFieldType.Numeric, FieldLength = 3, DecimalCount = 0, SortOrder = 3 },
                new() { ExcelColumnName = "成绩", DbfFieldName = "CJ", DbfFieldType = DbfFieldType.Numeric, FieldLength = 5, DecimalCount = 2, SortOrder = 4 },
            },
        };
    }

    // ── 模板 CRUD ──

    [Fact]
    public async Task Template_SaveAndRetrieve_RoundTrip()
    {
        var svc = _services.GetRequiredService<ITemplateService>();
        var template = CreateTestTemplate("学生模板");

        int id = await svc.SaveAsync(template);
        Assert.True(id > 0);

        var loaded = await svc.GetByIdAsync(id);
        Assert.NotNull(loaded);
        Assert.Equal("学生模板", loaded!.Name);
        Assert.Equal(4, loaded.Fields.Count);
        Assert.Equal("XM", loaded.Fields[0].DbfFieldName);
        Assert.Equal(DbfFieldType.Character, loaded.Fields[0].DbfFieldType);
        Assert.Equal(20, loaded.Fields[0].FieldLength);
    }

    [Fact]
    public async Task Template_Delete_RemovesFromStorage()
    {
        var svc = _services.GetRequiredService<ITemplateService>();
        int id = await svc.SaveAsync(CreateTestTemplate());
        await svc.DeleteAsync(id);

        var loaded = await svc.GetByIdAsync(id);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task Template_GetAll_ReturnsAllTemplates()
    {
        var svc = _services.GetRequiredService<ITemplateService>();
        await svc.SaveAsync(CreateTestTemplate("模板A"));
        await svc.SaveAsync(CreateTestTemplate("模板B"));
        await svc.SaveAsync(CreateTestTemplate("模板C"));

        var all = await svc.GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, t => t.Name == "模板A");
        Assert.Contains(all, t => t.Name == "模板B");
        Assert.Contains(all, t => t.Name == "模板C");
    }

    // ── 模板导入/导出 ──

    [Fact]
    public async Task Template_ExportAndImport_ProducesIdenticalTemplate()
    {
        var svc = _services.GetRequiredService<ITemplateService>();
        var original = CreateTestTemplate("导出测试模板");
        await svc.SaveAsync(original);

        string exportPath = Path.Combine(_testDir, "export.exdbftemplate");
        await svc.ExportAsync(original, exportPath);
        Assert.True(File.Exists(exportPath));

        await svc.DeleteAsync(original.Id);

        var imported = await svc.ImportAsync(exportPath);
        Assert.Equal("导出测试模板", imported.Name);
        Assert.Equal(4, imported.Fields.Count);

        var originalField = original.Fields[0];
        var importedField = imported.Fields[0];
        Assert.Equal(originalField.ExcelColumnName, importedField.ExcelColumnName);
        Assert.Equal(originalField.DbfFieldName, importedField.DbfFieldName);
        Assert.Equal(originalField.DbfFieldType, importedField.DbfFieldType);
        Assert.Equal(originalField.FieldLength, importedField.FieldLength);
        Assert.Equal(originalField.DecimalCount, importedField.DecimalCount);
    }

    [Fact]
    public async Task Template_Export_ProducesValidJson()
    {
        var svc = _services.GetRequiredService<ITemplateService>();
        var template = CreateTestTemplate("JSON测试");
        await svc.SaveAsync(template);

        string exportPath = Path.Combine(_testDir, "json_test.exdbftemplate");
        await svc.ExportAsync(template, exportPath);

        string json = File.ReadAllText(exportPath);
        Assert.Contains("JSON", json);
        Assert.Contains("XM", json);
        Assert.Contains("SFZH", json);
        Assert.Contains("NL", json);
        Assert.Contains("CJ", json);
    }

    // ── 模板复制 ──

    [Fact]
    public async Task Template_Copy_ProducesIndependentDuplicate()
    {
        var svc = _services.GetRequiredService<ITemplateService>();
        var original = CreateTestTemplate("原始模板");
        await svc.SaveAsync(original);

        var copy = new TemplateInfo
        {
            Name = original.Name + "(副本)",
            Description = original.Description,
            Version = original.Version,
            Fields = original.Fields.Select(f => new TemplateFieldInfo
            {
                Id = 0,
                TemplateId = 0,
                ExcelColumnName = f.ExcelColumnName,
                DbfFieldName = f.DbfFieldName,
                DbfFieldType = f.DbfFieldType,
                FieldLength = f.FieldLength,
                DecimalCount = f.DecimalCount,
                IsRequired = f.IsRequired,
                SortOrder = f.SortOrder,
            }).ToList(),
        };

        int copyId = await svc.SaveAsync(copy);
        Assert.NotEqual(original.Id, copyId);

        var loadedOriginal = await svc.GetByIdAsync(original.Id);
        var loadedCopy = await svc.GetByIdAsync(copyId);
        Assert.NotNull(loadedOriginal);
        Assert.NotNull(loadedCopy);
        Assert.Equal("原始模板", loadedOriginal!.Name);
        Assert.Equal("原始模板(副本)", loadedCopy!.Name);
        Assert.Equal(4, loadedCopy.Fields.Count);
    }

    // ── 模板匹配应用 ──

    [Fact]
    public async Task Template_Apply_MatchesColumnsByExcelName()
    {
        var svc = _services.GetRequiredService<ITemplateService>();
        var template = CreateTestTemplate("匹配测试");
        await svc.SaveAsync(template);

        var sourceMappings = new List<FieldMapping>
        {
            new() { ExcelColumnName = "姓名", ExcelColumnIndex = 0, DbfFieldName = "XM", DbfFieldType = DbfFieldType.Character, Length = 20, IsExport = true },
            new() { ExcelColumnName = "身份证号", ExcelColumnIndex = 1, DbfFieldName = "SFZH", DbfFieldType = DbfFieldType.Character, Length = 18, IsExport = true },
            new() { ExcelColumnName = "联系电话", ExcelColumnIndex = 2, DbfFieldName = "LXDH", DbfFieldType = DbfFieldType.Character, Length = 20, IsExport = true },
        };

        var (mappings, missing, unused) = svc.ApplyTemplate(template, sourceMappings);

        Assert.Equal(4, mappings.Count);
        Assert.Equal(2, missing.Count);
        Assert.Contains("年龄", missing);
        Assert.Contains("成绩", missing);
        Assert.Single(unused);
        Assert.Equal("联系电话", unused[0]);

        var xmMapping = mappings.First(m => m.DbfFieldName == "XM");
        Assert.Equal(20, xmMapping.Length);
        Assert.Equal(DbfFieldType.Character, xmMapping.DbfFieldType);
    }

    // ── 历史记录 ──

    [Fact]
    public async Task History_AddAndRetrieve_RoundTrip()
    {
        var svc = _services.GetRequiredService<IHistoryService>();
        var entry = new ConversionHistoryEntry
        {
            ConversionType = ConversionType.ExcelToDbf,
            SourceFileName = "test.xlsx",
            SourceFilePath = "C:\\test\\test.xlsx",
            TargetFileName = "test.dbf",
            TargetFilePath = "C:\\test\\test.dbf",
            Status = ConversionStatus.Success,
            TotalRows = 100,
            SuccessRows = 100,
            FailedRows = 0,
            ElapsedMilliseconds = 1500,
        };

        await svc.AddAsync(entry);

        var recent = await svc.GetRecentAsync(1);
        Assert.Single(recent);
        Assert.Equal("test.xlsx", recent[0].SourceFileName);
        Assert.Equal(ConversionType.ExcelToDbf, recent[0].ConversionType);
        Assert.Equal(ConversionStatus.Success, recent[0].Status);
        Assert.Equal(100, recent[0].SuccessRows);
    }

    [Fact]
    public async Task History_GetAll_ReturnsAllEntries()
    {
        var svc = _services.GetRequiredService<IHistoryService>();

        for (int i = 1; i <= 5; i++)
        {
            await svc.AddAsync(new ConversionHistoryEntry
            {
                ConversionType = ConversionType.ExcelToDbf,
                SourceFileName = $"file{i}.xlsx",
                Status = ConversionStatus.Success,
                SuccessRows = i * 10,
            });
        }

        var all = await svc.GetAllAsync();
        Assert.Equal(5, all.Count);
    }

    [Fact]
    public async Task History_Delete_RemovesEntry()
    {
        var svc = _services.GetRequiredService<IHistoryService>();
        var entry = new ConversionHistoryEntry
        {
            ConversionType = ConversionType.DbfToExcel,
            SourceFileName = "delete_me.dbf",
            Status = ConversionStatus.Success,
        };

        await svc.AddAsync(entry);
        var all = await svc.GetAllAsync();
        int id = all[0].Id;

        await svc.DeleteAsync(id);
        var after = await svc.GetAllAsync();
        Assert.DoesNotContain(after, e => e.Id == id);
    }

    [Fact]
    public async Task History_Clear_RemovesAllEntries()
    {
        var svc = _services.GetRequiredService<IHistoryService>();
        for (int i = 0; i < 3; i++)
        {
            await svc.AddAsync(new ConversionHistoryEntry
            {
                ConversionType = ConversionType.ExcelToDbf,
                SourceFileName = $"clear{i}.xlsx",
                Status = ConversionStatus.Success,
            });
        }

        await svc.ClearAsync();
        var all = await svc.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task History_GetRecent_LimitsCount()
    {
        var svc = _services.GetRequiredService<IHistoryService>();
        for (int i = 0; i < 10; i++)
        {
            await svc.AddAsync(new ConversionHistoryEntry
            {
                ConversionType = ConversionType.ExcelToDbf,
                SourceFileName = $"recent{i}.xlsx",
                Status = ConversionStatus.Success,
            });
        }

        var recent = await svc.GetRecentAsync(5);
        Assert.Equal(5, recent.Count);
    }

    // ── 设置持久化 ──

    [Fact]
    public async Task Settings_SaveAndLoad_RoundTrip()
    {
        var svc = _services.GetRequiredService<ISettingsService>();

        var settings = new AppSettings
        {
            DefaultOutputDirectory = "D:\\Output",
            DefaultExcelFormat = ExcelFormat.Xls,
            DefaultDbfEncoding = DbfEncodingKind.Gbk,
            PreviewRowCount = 500,
            TextKeywords = new List<string> { "编号", "身份证", "手机号" },
        };

        await svc.SaveAsync(settings);
        var loaded = await svc.LoadAsync();

        Assert.Equal("D:\\Output", loaded.DefaultOutputDirectory);
        Assert.Equal(ExcelFormat.Xls, loaded.DefaultExcelFormat);
        Assert.Equal(DbfEncodingKind.Gbk, loaded.DefaultDbfEncoding);
        Assert.Equal(500, loaded.PreviewRowCount);
        Assert.Equal(3, loaded.TextKeywords.Count);
    }
}
