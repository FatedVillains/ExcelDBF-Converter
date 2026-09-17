namespace ExcelDbfConverter.Core.Constants;

/// <summary>Excel 相关常量。</summary>
public static class ExcelConstants
{
    /// <summary>.xlsx 单 Sheet 最大行数（含表头）。</summary>
    public const int XlsxMaxRows = 1_048_576;

    /// <summary>.xls 单 Sheet 最大行数（含表头）。</summary>
    public const int XlsMaxRows = 65_536;

    /// <summary>Sheet 名称最大长度。</summary>
    public const int MaxSheetNameLength = 31;

    /// <summary>Sheet 名称非法字符。</summary>
    public static readonly char[] InvalidSheetNameChars = { '\\', '/', '?', '*', '[', ']', ':' };

    /// <summary>默认 Sheet 名称。</summary>
    public const string DefaultSheetName = "数据导出";
}
