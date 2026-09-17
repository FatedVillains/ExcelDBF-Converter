using ExcelDbfConverter.Application;
using ExcelDbfConverter.Core.DTOs;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Infrastructure;
using ExcelDbfConverter.Infrastructure.Configuration;
using ExcelDbfConverter.Infrastructure.SQLite;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelDbfConverter.Integration.Tests;

/// <summary>转换流程集成测试：Excel→DBF→Excel 往返、错误处理。</summary>
public class ConversionIntegrationTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly string _testDir;

    public ConversionIntegrationTests()
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

    // ── Excel → DBF 转换测试 ──

    [Fact]
    public async Task ExcelToDbf_FullTypes_ProducesValidDbf()
    {
        var excelPath = TestDataFactory.CreateFullTypeExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "output.dbf");

        var request = new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetFullTypeMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.Stop,
        };

        var result = await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(request);

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.Equal(3, result.SuccessRows);
        Assert.Equal(0, result.FailedRows);
        Assert.True(File.Exists(dbfPath));
    }

    [Fact]
    public async Task ExcelToDbf_FullTypes_DbStructureMatchesSchema()
    {
        var excelPath = TestDataFactory.CreateFullTypeExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "structure.dbf");

        var request = new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetFullTypeMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.Stop,
        };

        await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(request);

        var info = await _services.GetRequiredService<IDbfReader>().ReadStructureAsync(dbfPath);

        Assert.Equal(9, info.Schema.Fields.Count);
        Assert.Equal(3, info.RecordCount);
        Assert.Equal("XM", info.Schema.Fields[0].Name);
        Assert.Equal(DbfFieldType.Character, info.Schema.Fields[0].Type);
        Assert.Equal(20, info.Schema.Fields[0].Length);
        Assert.Equal(DbfFieldType.Numeric, info.Schema.Fields[2].Type);
        Assert.Equal(3, info.Schema.Fields[2].Length);
        Assert.Equal(0, info.Schema.Fields[2].DecimalCount);
        Assert.Equal(DbfFieldType.DateTime, info.Schema.Fields[5].Type);
        Assert.Equal(DbfFieldType.Logical, info.Schema.Fields[6].Type);
        Assert.Equal(DbfFieldType.Integer, info.Schema.Fields[7].Type);
        Assert.Equal(DbfFieldType.Double, info.Schema.Fields[8].Type);
    }

    [Fact]
    public async Task ExcelToDbf_FullTypes_DbDataMatchesSource()
    {
        var excelPath = TestDataFactory.CreateFullTypeExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "data.dbf");

        await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetFullTypeMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.Stop,
        });

        var records = new List<DbfRecord>();
        await foreach (var record in _services.GetRequiredService<IDbfReader>().ReadRecordsAsync(dbfPath, new DbfReadOptions { Encoding = DbfEncodingKind.Gbk }))
        {
            records.Add(record);
        }

        Assert.Equal(3, records.Count);
        Assert.Equal("张三", records[0].Values[0]);
        Assert.Equal("李四", records[1].Values[0]);
        Assert.Equal("王五", records[2].Values[0]);
        Assert.Equal("140101199001011234", records[0].Values[1]);
        Assert.Equal(20m, records[0].Values[2]);
        Assert.Equal(90.5m, records[0].Values[3]);
        Assert.True(records[0].Values[4] is DateTime dt0 && dt0.Date == new DateTime(2000, 1, 1));
        Assert.True(records[0].Values[6] is true);
        Assert.True(records[1].Values[6] is false);
        Assert.Equal(1, records[0].Values[7]);
        Assert.Equal(2, records[1].Values[7]);
    }

    // ── DBF → Excel 转换测试 ──

    [Fact]
    public async Task DbfToExcel_ProducesValidExcel()
    {
        var excelPath = TestDataFactory.CreateFullTypeExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "roundtrip.dbf");
        string excelOutPath = Path.Combine(_testDir, "roundtrip.xlsx");

        await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetFullTypeMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.Stop,
        });

        var result = await _services.GetRequiredService<IConversionService>().ConvertDbfToExcelAsync(new DbfToExcelRequest
        {
            SourcePath = dbfPath,
            OutputPath = excelOutPath,
            ExcelFormat = ExcelFormat.Xlsx,
            SheetName = "导出数据",
            Encoding = DbfEncodingKind.Auto,
        });

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.True(File.Exists(excelOutPath));
        Assert.Equal(3, result.SuccessRows);
    }

    [Fact]
    public async Task DbfToExcel_XlsFormat_ProducesValidFile()
    {
        var excelPath = TestDataFactory.CreateFullTypeExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "xls_test.dbf");
        string xlsPath = Path.Combine(_testDir, "xls_output.xls");

        await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetFullTypeMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.Stop,
        });

        var result = await _services.GetRequiredService<IConversionService>().ConvertDbfToExcelAsync(new DbfToExcelRequest
        {
            SourcePath = dbfPath,
            OutputPath = xlsPath,
            ExcelFormat = ExcelFormat.Xls,
            SheetName = "数据",
            Encoding = DbfEncodingKind.Gbk,
        });

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.True(File.Exists(xlsPath));
    }

    // ── 错误处理测试 ──

    [Fact]
    public async Task ErrorHandling_Stop_FailsOnFirstError()
    {
        var excelPath = TestDataFactory.CreateBadDataExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "stop.dbf");

        var result = await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetBadDataMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.Stop,
        });

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.Contains("NL", result.Message ?? "");
    }

    [Fact]
    public async Task ErrorHandling_SkipRow_SkipsBadRows()
    {
        var excelPath = TestDataFactory.CreateBadDataExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "skip.dbf");

        var result = await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetBadDataMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.SkipRow,
        });

        Assert.Equal(ConversionStatus.Partial, result.Status);
        Assert.Equal(2, result.SuccessRows);
        Assert.Equal(2, result.FailedRows);
    }

    [Fact]
    public async Task ErrorHandling_SetNull_KeepsRowsWithNullFields()
    {
        var excelPath = TestDataFactory.CreateBadDataExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "setnull.dbf");

        var result = await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetBadDataMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.SetNull,
        });

        Assert.Equal(ConversionStatus.Partial, result.Status);
        Assert.Equal(4, result.SuccessRows);
    }

    [Fact]
    public async Task ErrorHandling_ErrorReport_IsGenerated()
    {
        var excelPath = TestDataFactory.CreateBadDataExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "report.dbf");

        var result = await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetBadDataMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.SkipRow,
        });

        Assert.NotNull(result.ErrorReportPath);
        Assert.True(File.Exists(result.ErrorReportPath!));
    }

    // ── 边界场景测试 ──

    [Fact]
    public async Task ExcelToDbf_EmptyExcel_SucceedsWithZeroRows()
    {
        var excelPath = TestDataFactory.CreateEmptyExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "empty.dbf");

        var result = await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = new List<FieldMapping>
            {
                new() { ExcelColumnName = "NAME", ExcelColumnIndex = 0, DbfFieldName = "XM", DbfFieldType = DbfFieldType.Character, Length = 20, IsExport = true },
                new() { ExcelColumnName = "VALUE", ExcelColumnIndex = 1, DbfFieldName = "ZHI", DbfFieldType = DbfFieldType.Character, Length = 50, IsExport = true },
            },
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.Stop,
        });

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.Equal(0, result.SuccessRows);
    }

    [Fact]
    public async Task ExcelToDbf_NoExportFields_Fails()
    {
        var excelPath = TestDataFactory.CreateFullTypeExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "noexport.dbf");

        var result = await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetFullTypeMappings().Select(m =>
            {
                m.IsExport = false;
                return m;
            }).ToList(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.Stop,
        });

        Assert.Equal(ConversionStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExcelToDbf_GbkEncoding_ChineseReadsCorrectly()
    {
        var excelPath = TestDataFactory.CreateFullTypeExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "gbk.dbf");

        await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(new ExcelToDbfRequest
        {
            SourcePath = excelPath,
            SheetName = "Sheet1",
            OutputPath = dbfPath,
            FieldMappings = TestDataFactory.GetFullTypeMappings(),
            Encoding = DbfEncodingKind.Gbk,
            ErrorHandling = ErrorHandlingMode.Stop,
        });

        var records = new List<DbfRecord>();
        await foreach (var record in _services.GetRequiredService<IDbfReader>().ReadRecordsAsync(dbfPath, new DbfReadOptions { Encoding = DbfEncodingKind.Gbk }))
        {
            records.Add(record);
        }

        Assert.Equal("张三", records[0].Values[0]);
        Assert.Equal("李四", records[1].Values[0]);
        Assert.Equal("王五", records[2].Values[0]);
    }

    [Fact]
    public async Task ExcelToDbf_Cancellation_StopsConversion()
    {
        var excelPath = TestDataFactory.CreateFullTypeExcel(_testDir);
        string dbfPath = Path.Combine(_testDir, "cancel.dbf");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _services.GetRequiredService<IConversionService>().ConvertExcelToDbfAsync(
            new ExcelToDbfRequest
            {
                SourcePath = excelPath,
                SheetName = "Sheet1",
                OutputPath = dbfPath,
                FieldMappings = TestDataFactory.GetFullTypeMappings(),
                Encoding = DbfEncodingKind.Gbk,
                ErrorHandling = ErrorHandlingMode.Stop,
            },
            cancellationToken: cts.Token);

        Assert.Equal(ConversionStatus.Cancelled, result.Status);
    }

    // ── 批量转换测试 ──

    [Fact]
    public async Task BatchConvert_MultipleFiles_Succeeds()
    {
        var excel1 = TestDataFactory.CreateFullTypeExcel(_testDir);
        var excel2 = Path.Combine(_testDir, "second.xlsx");
        File.Copy(excel1, excel2);

        string outputDir = Path.Combine(_testDir, "batch_output");
        Directory.CreateDirectory(outputDir);

        var request = new BatchConversionRequest
        {
            ConversionType = ConversionType.ExcelToDbf,
            SourceFiles = new List<string> { excel1, excel2 },
            OutputDirectory = outputDir,
            OverwriteMode = OverwriteMode.Rename,
            ErrorHandling = ErrorHandlingMode.Stop,
            Encoding = DbfEncodingKind.Gbk,
            ExcelFormat = ExcelFormat.Xlsx,
        };

        var results = await _services.GetRequiredService<IConversionService>().BatchConvertAsync(request);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(ConversionStatus.Success, r.Status));
    }

    [Fact]
    public async Task BatchConvert_RenameMode_ProducesUniqueFiles()
    {
        var excel = TestDataFactory.CreateFullTypeExcel(_testDir);
        string outputDir = Path.Combine(_testDir, "rename_output");
        Directory.CreateDirectory(outputDir);

        var request = new BatchConversionRequest
        {
            ConversionType = ConversionType.ExcelToDbf,
            SourceFiles = new List<string> { excel },
            OutputDirectory = outputDir,
            OverwriteMode = OverwriteMode.Rename,
            ErrorHandling = ErrorHandlingMode.Stop,
            Encoding = DbfEncodingKind.Gbk,
            ExcelFormat = ExcelFormat.Xlsx,
        };

        await _services.GetRequiredService<IConversionService>().BatchConvertAsync(request);
        await _services.GetRequiredService<IConversionService>().BatchConvertAsync(request);

        var files = Directory.GetFiles(outputDir, "*.dbf");
        Assert.Equal(2, files.Length);
    }

    [Fact]
    public async Task BatchConvert_SkipMode_KeepsExistingFiles()
    {
        var excel = TestDataFactory.CreateFullTypeExcel(_testDir);
        string outputDir = Path.Combine(_testDir, "skip_output");
        Directory.CreateDirectory(outputDir);

        var request = new BatchConversionRequest
        {
            ConversionType = ConversionType.ExcelToDbf,
            SourceFiles = new List<string> { excel },
            OutputDirectory = outputDir,
            OverwriteMode = OverwriteMode.Skip,
            ErrorHandling = ErrorHandlingMode.Stop,
            Encoding = DbfEncodingKind.Gbk,
            ExcelFormat = ExcelFormat.Xlsx,
        };

        await _services.GetRequiredService<IConversionService>().BatchConvertAsync(request);
        var secondResults = await _services.GetRequiredService<IConversionService>().BatchConvertAsync(request);

        Assert.Equal(ConversionStatus.Failed, secondResults[0].Status);
        Assert.Contains("跳过", secondResults[0].Message ?? "");
    }
}
