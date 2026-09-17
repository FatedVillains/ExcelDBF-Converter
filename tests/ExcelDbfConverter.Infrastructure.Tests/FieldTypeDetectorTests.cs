using ExcelDbfConverter.Application.Services;
using ExcelDbfConverter.Core.Constants;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Infrastructure.Tests;

public class FieldTypeDetectorTests
{
    private readonly FieldTypeDetector _detector = new();

    private static IReadOnlyList<ExcelColumnInfo> Columns(params string[] names) =>
        names.Select((n, i) => new ExcelColumnInfo { Name = n, Index = i }).ToList();

    private static ExcelRow Row(params object?[] values) => new() { RowNumber = 1, Values = values.ToList() };

    [Fact]
    public void Detect_AllNumeric_RecognizesNumeric()
    {
        var columns = Columns("成绩");
        var rows = new List<ExcelRow> { Row(90.5), Row(88.25), Row(100.00) };

        var result = _detector.Detect(columns, rows, AppDefaults.DefaultTextKeywords);

        Assert.Single(result);
        Assert.Equal(DbfFieldType.Numeric, result[0].DbfFieldType);
        Assert.True(result[0].DecimalCount > 0);
    }

    [Fact]
    public void Detect_IdNumberKeyword_ForcesCharacter()
    {
        var columns = Columns("身份证号");
        var rows = new List<ExcelRow> { Row("140101199001011234"), Row("140101199001011235") };

        var result = _detector.Detect(columns, rows, AppDefaults.DefaultTextKeywords);

        Assert.Single(result);
        Assert.Equal(DbfFieldType.Character, result[0].DbfFieldType);
    }

    [Fact]
    public void Detect_DateStrings_RecognizesDate()
    {
        var columns = Columns("出生日期");
        var rows = new List<ExcelRow> { Row("2000-01-01"), Row("1999/02/02") };

        var result = _detector.Detect(columns, rows, AppDefaults.DefaultTextKeywords);

        Assert.Single(result);
        Assert.Equal(DbfFieldType.Date, result[0].DbfFieldType);
    }

    [Fact]
    public void Detect_DateTimeStrings_RecognizesDateTime()
    {
        var columns = Columns("创建时间");
        var rows = new List<ExcelRow> { Row("2024-05-20 10:30:00"), Row("2024-05-21 11:00:00") };

        var result = _detector.Detect(columns, rows, AppDefaults.DefaultTextKeywords);

        Assert.Single(result);
        Assert.Equal(DbfFieldType.DateTime, result[0].DbfFieldType);
    }

    [Fact]
    public void Detect_LogicalValues_RecognizesLogical()
    {
        var columns = Columns("是否有效");
        var rows = new List<ExcelRow> { Row("是"), Row("否"), Row("是") };

        var result = _detector.Detect(columns, rows, AppDefaults.DefaultTextKeywords);

        Assert.Single(result);
        Assert.Equal(DbfFieldType.Logical, result[0].DbfFieldType);
    }

    [Fact]
    public void Detect_ChineseText_RecognizesCharacter()
    {
        var columns = Columns("姓名");
        var rows = new List<ExcelRow> { Row("张三"), Row("李四") };

        var result = _detector.Detect(columns, rows, AppDefaults.DefaultTextKeywords);

        Assert.Single(result);
        Assert.Equal(DbfFieldType.Character, result[0].DbfFieldType);
    }
}
