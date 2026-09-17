using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Models;
using ExcelDbfConverter.Infrastructure.Dbf;

namespace ExcelDbfConverter.Infrastructure.Tests;

public class DbfRoundTripTests
{
    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), $"dbf_test_{Guid.NewGuid():N}.dbf");

    [Fact]
    public async Task WriteAndRead_AllFieldTypes_RoundTrip()
    {
        string path = TempPath();
        try
        {
            var schema = new DbfTableSchema
            {
                Fields = new List<DbfFieldDefinition>
                {
                    new() { Name = "NAME", Type = DbfFieldType.Character, Length = 30 },
                    new() { Name = "AGE", Type = DbfFieldType.Numeric, Length = 10, DecimalCount = 2 },
                    new() { Name = "SALARY", Type = DbfFieldType.Float, Length = 12, DecimalCount = 2 },
                    new() { Name = "BIRTH", Type = DbfFieldType.Date, Length = 8 },
                    new() { Name = "CREATED", Type = DbfFieldType.DateTime, Length = 8 },
                    new() { Name = "ACTIVE", Type = DbfFieldType.Logical, Length = 1 },
                    new() { Name = "COUNT", Type = DbfFieldType.Integer, Length = 4 },
                    new() { Name = "RATIO", Type = DbfFieldType.Double, Length = 8 },
                },
            };

            var writer = new DbfWriter();
            await writer.CreateAsync(path, schema, DbfEncodingKind.Gbk);

            var birth = new DateTime(2000, 1, 1);
            var created = new DateTime(2024, 5, 20, 10, 30, 15);
            await writer.WriteRecordAsync(new DbfRecord
            {
                Values = new List<object?> { "张三", 18.5m, 1234.56m, birth, created, true, 42, 3.14159265358979 },
            });
            await writer.WriteRecordAsync(new DbfRecord
            {
                Values = new List<object?> { "李四", 99m, 9876.54m, birth.AddDays(1), created, false, -7, 2.5 },
            });
            await writer.CompleteAsync();
            writer.Dispose();

            var reader = new DbfReader();
            var info = await reader.ReadStructureAsync(path);
            Assert.Equal(8, info.Schema.Fields.Count);
            Assert.Equal(2, info.RecordCount);

            var records = new List<DbfRecord>();
            await foreach (var record in reader.ReadRecordsAsync(path, new DbfReadOptions { Encoding = DbfEncodingKind.Gbk }))
            {
                records.Add(record);
            }

            Assert.Equal(2, records.Count);

            // 字符型
            Assert.Equal("张三", records[0].Values[0]);
            Assert.Equal("李四", records[1].Values[0]);

            // 数值型
            Assert.Equal(18.5m, records[0].Values[1]);
            Assert.Equal(99m, records[1].Values[1]);

            // 浮点型
            Assert.Equal(1234.56m, records[0].Values[2]);

            // 日期型
            Assert.Equal(birth, records[0].Values[3]);

            // 日期时间型（精确到秒，毫秒可能因存储截断）
            var createdRead = Assert.IsType<DateTime>(records[0].Values[4]);
            Assert.Equal(created.Date, createdRead.Date);
            Assert.Equal(created.Hour, createdRead.Hour);
            Assert.Equal(created.Minute, createdRead.Minute);
            Assert.Equal(created.Second, createdRead.Second);

            // 逻辑型
            Assert.Equal(true, records[0].Values[5]);
            Assert.Equal(false, records[1].Values[5]);

            // 整型
            Assert.Equal(42, records[0].Values[6]);
            Assert.Equal(-7, records[1].Values[6]);

            // 双精度型
            Assert.Equal(3.14159265358979, Assert.IsType<double>(records[0].Values[7]), 10);
            Assert.Equal(2.5, Assert.IsType<double>(records[1].Values[7]), 10);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAndRead_ChineseGbk_RoundTrip()
    {
        string path = TempPath();
        try
        {
            var schema = new DbfTableSchema
            {
                Fields = new List<DbfFieldDefinition>
                {
                    new() { Name = "XM", Type = DbfFieldType.Character, Length = 20 },
                    new() { Name = "BZ", Type = DbfFieldType.Character, Length = 50 },
                },
            };

            var writer = new DbfWriter();
            await writer.CreateAsync(path, schema, DbfEncodingKind.Gbk);
            await writer.WriteRecordAsync(new DbfRecord
            {
                Values = new List<object?> { "王小明", "中华人民共和国北京市朝阳区" },
            });
            await writer.CompleteAsync();
            writer.Dispose();

            var reader = new DbfReader();
            var info = await reader.ReadStructureAsync(path);
            // GBK 语言驱动字节应为 0x7A。
            Assert.Equal(0x7A, info.LanguageDriver);

            var records = new List<DbfRecord>();
            await foreach (var record in reader.ReadRecordsAsync(path, new DbfReadOptions { Encoding = DbfEncodingKind.Gbk }))
            {
                records.Add(record);
            }

            Assert.Single(records);
            Assert.Equal("王小明", records[0].Values[0]);
            Assert.Equal("中华人民共和国北京市朝阳区", records[0].Values[1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DateTimeCodec_KnownValue_MatchesJulianEpoch()
    {
        var date = new DateTime(2000, 1, 1);
        int jdn = DbfDateTimeCodec.ToJulianDay(date);
        Assert.Equal(2451545, jdn);
        Assert.Equal(date, DbfDateTimeCodec.FromJulianDay(jdn));
    }

    [Fact]
    public async Task NumericOverflow_WritesAsterisks()
    {
        string path = TempPath();
        try
        {
            var schema = new DbfTableSchema
            {
                Fields = new List<DbfFieldDefinition>
                {
                    new() { Name = "N", Type = DbfFieldType.Numeric, Length = 3, DecimalCount = 0 },
                },
            };

            var writer = new DbfWriter();
            await writer.CreateAsync(path, schema, DbfEncodingKind.Gbk);
            await writer.WriteRecordAsync(new DbfRecord { Values = new List<object?> { 12345m } });
            await writer.CompleteAsync();
            writer.Dispose();

            var reader = new DbfReader();
            await foreach (var record in reader.ReadRecordsAsync(path, new DbfReadOptions()))
            {
                // 溢出时 VFP 写星号，读取应为空。
                Assert.Null(record.Values[0]);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}
