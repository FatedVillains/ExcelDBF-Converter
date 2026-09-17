using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ExcelDbfConverter.Integration.Tests;

/// <summary>集成测试数据工厂：创建测试 Excel 文件与 DI 容器。</summary>
public static class TestDataFactory
{
    /// <summary>生成唯一测试目录。</summary>
    public static string EnsureTestDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"edbc_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>清理测试目录。</summary>
    public static void CleanupTestDir(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
    }

    /// <summary>生成包含全类型数据的测试 Excel 文件。</summary>
    public static string CreateFullTypeExcel(string testDir)
    {
        string path = Path.Combine(testDir, "full_type_test.xlsx");
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("Sheet1");

        var headers = new[] { "姓名", "身份证号", "年龄", "成绩", "出生日期", "创建时间", "是否有效", "序号", "比例" };
        for (int i = 0; i < headers.Length; i++)
            sheet.CreateRow(0).CreateCell(i).SetCellValue(headers[i]);

        var data = new object?[][]
        {
            new object?[] { "张三", "140101199001011234", 20, 90.5, new DateTime(2000, 1, 1), new DateTime(2024, 5, 20, 10, 30, 15), true, 1, 3.14 },
            new object?[] { "李四", "140101199002022345", 21, 88.0, new DateTime(1999, 2, 2), new DateTime(2024, 5, 20, 11, 0, 0), false, 2, 2.5 },
            new object?[] { "王五", "140101199003033456", 22, 75.5, new DateTime(2001, 6, 15), new DateTime(2024, 6, 1, 8, 0, 0), true, 3, 1.618 },
        };

        for (int r = 0; r < data.Length; r++)
        {
            var row = sheet.CreateRow(r + 1);
            for (int c = 0; c < data[r].Length; c++)
            {
                var cell = row.CreateCell(c);
                var val = data[r][c];
                if (val is string s)
                {
                    cell.SetCellValue(s);
                }
                else if (val is int i)
                {
                    cell.SetCellValue((double)i);
                }
                else if (val is double d)
                {
                    cell.SetCellValue(d);
                }
                else if (val is DateTime dt && c == 4)
                {
                    cell.SetCellValue(dt);
                    var style = workbook.CreateCellStyle();
                    style.DataFormat = workbook.CreateDataFormat().GetFormat("yyyy-mm-dd");
                    cell.CellStyle = style;
                }
                else if (val is DateTime dt2 && c == 5)
                {
                    cell.SetCellValue(dt2);
                    var style2 = workbook.CreateCellStyle();
                    style2.DataFormat = workbook.CreateDataFormat().GetFormat("yyyy-mm-dd hh:mm:ss");
                    cell.CellStyle = style2;
                }
                else if (val is bool b)
                {
                    cell.SetCellValue(b);
                }
            }
        }

        using var fs = File.Create(path);
        workbook.Write(fs);
        return path;
    }

    /// <summary>生成包含非法数据的 Excel 文件（用于错误处理测试）。</summary>
    public static string CreateBadDataExcel(string testDir)
    {
        string path = Path.Combine(testDir, "bad_data_test.xlsx");
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("Sheet1");

        var headers = new[] { "NAME", "AGE", "SCORE" };
        for (int i = 0; i < headers.Length; i++)
            sheet.CreateRow(0).CreateCell(i).SetCellValue(headers[i]);

        var rows = new object?[][]
        {
            new object?[] { "张三", 20, 90.5 },
            new object?[] { "李四", "ABC", 88.0 },
            new object?[] { "王五", 22, "N/A" },
            new object?[] { "赵六", 23, 75.0 },
        };

        for (int r = 0; r < rows.Length; r++)
        {
            var row = sheet.CreateRow(r + 1);
            for (int c = 0; c < rows[r].Length; c++)
            {
                var cell = row.CreateCell(c);
                if (rows[r][c] is string s)
                {
                    cell.SetCellValue(s);
                }
                else
                {
                    cell.SetCellValue(Convert.ToDouble(rows[r][c]));
                }
            }
        }

        using var fs = File.Create(path);
        workbook.Write(fs);
        return path;
    }

    /// <summary>生成空 Excel 文件。</summary>
    public static string CreateEmptyExcel(string testDir)
    {
        string path = Path.Combine(testDir, "empty_test.xlsx");
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("Sheet1");
        sheet.CreateRow(0).CreateCell(0).SetCellValue("NAME");
        sheet.CreateRow(0).CreateCell(1).SetCellValue("VALUE");
        using var fs = File.Create(path);
        workbook.Write(fs);
        return path;
    }

    /// <summary>生成多 Sheet 的 Excel 文件。</summary>
    public static string CreateMultiSheetExcel(string testDir)
    {
        string path = Path.Combine(testDir, "multi_sheet_test.xlsx");
        using var workbook = new XSSFWorkbook();

        var sheet1 = workbook.CreateSheet("学生数据");
        sheet1.CreateRow(0).CreateCell(0).SetCellValue("姓名");
        sheet1.CreateRow(0).CreateCell(1).SetCellValue("成绩");
        sheet1.CreateRow(1).CreateCell(0).SetCellValue("张三");
        sheet1.CreateRow(1).CreateCell(1).SetCellValue(90.5);

        var sheet2 = workbook.CreateSheet("教师数据");
        sheet2.CreateRow(0).CreateCell(0).SetCellValue("姓名");
        sheet2.CreateRow(0).CreateCell(1).SetCellValue("职称");
        sheet2.CreateRow(1).CreateCell(0).SetCellValue("李老师");
        sheet2.CreateRow(1).CreateCell(1).SetCellValue("教授");

        using var fs = File.Create(path);
        workbook.Write(fs);
        return path;
    }

    /// <summary>创建全类型字段映射列表。</summary>
    public static List<FieldMapping> GetFullTypeMappings() => new()
    {
        new() { ExcelColumnName = "姓名", ExcelColumnIndex = 0, DbfFieldName = "XM", DbfFieldType = DbfFieldType.Character, Length = 20, DecimalCount = 0, IsExport = true },
        new() { ExcelColumnName = "身份证号", ExcelColumnIndex = 1, DbfFieldName = "SFZH", DbfFieldType = DbfFieldType.Character, Length = 18, DecimalCount = 0, IsExport = true },
        new() { ExcelColumnName = "年龄", ExcelColumnIndex = 2, DbfFieldName = "NL", DbfFieldType = DbfFieldType.Numeric, Length = 3, DecimalCount = 0, IsExport = true },
        new() { ExcelColumnName = "成绩", ExcelColumnIndex = 3, DbfFieldName = "CJ", DbfFieldType = DbfFieldType.Numeric, Length = 5, DecimalCount = 2, IsExport = true },
        new() { ExcelColumnName = "出生日期", ExcelColumnIndex = 4, DbfFieldName = "CSRQ", DbfFieldType = DbfFieldType.Date, Length = 8, DecimalCount = 0, IsExport = true },
        new() { ExcelColumnName = "创建时间", ExcelColumnIndex = 5, DbfFieldName = "CJSJ", DbfFieldType = DbfFieldType.DateTime, Length = 8, DecimalCount = 0, IsExport = true },
        new() { ExcelColumnName = "是否有效", ExcelColumnIndex = 6, DbfFieldName = "SFYX", DbfFieldType = DbfFieldType.Logical, Length = 1, DecimalCount = 0, IsExport = true },
        new() { ExcelColumnName = "序号", ExcelColumnIndex = 7, DbfFieldName = "XH", DbfFieldType = DbfFieldType.Integer, Length = 4, DecimalCount = 0, IsExport = true },
        new() { ExcelColumnName = "比例", ExcelColumnIndex = 8, DbfFieldName = "BL", DbfFieldType = DbfFieldType.Double, Length = 8, DecimalCount = 0, IsExport = true },
    };

    /// <summary>创建带非法数据的字段映射。</summary>
    public static List<FieldMapping> GetBadDataMappings() => new()
    {
        new() { ExcelColumnName = "NAME", ExcelColumnIndex = 0, DbfFieldName = "XM", DbfFieldType = DbfFieldType.Character, Length = 20, IsExport = true },
        new() { ExcelColumnName = "AGE", ExcelColumnIndex = 1, DbfFieldName = "NL", DbfFieldType = DbfFieldType.Numeric, Length = 3, DecimalCount = 0, IsExport = true },
        new() { ExcelColumnName = "SCORE", ExcelColumnIndex = 2, DbfFieldName = "CJ", DbfFieldType = DbfFieldType.Numeric, Length = 5, DecimalCount = 2, IsExport = true },
    };
}
