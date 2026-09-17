namespace ExcelDbfConverter.Core.Enums;

/// <summary>转换方向。</summary>
public enum ConversionType
{
    ExcelToDbf,
    DbfToExcel,
}

public static class ConversionTypeExtensions
{
    public static string DisplayName(this ConversionType type) => type switch
    {
        ConversionType.ExcelToDbf => "Excel → DBF",
        ConversionType.DbfToExcel => "DBF → Excel",
        _ => type.ToString(),
    };
}
