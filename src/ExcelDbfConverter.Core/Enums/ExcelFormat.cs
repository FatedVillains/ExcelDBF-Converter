namespace ExcelDbfConverter.Core.Enums;

/// <summary>Excel 输出格式。</summary>
public enum ExcelFormat
{
    Xlsx,
    Xls,
}

public static class ExcelFormatExtensions
{
    public static string FileExtension(this ExcelFormat format) =>
        format == ExcelFormat.Xls ? ".xls" : ".xlsx";

    public static string DisplayName(this ExcelFormat format) =>
        format == ExcelFormat.Xls ? "XLS" : "XLSX";
}
