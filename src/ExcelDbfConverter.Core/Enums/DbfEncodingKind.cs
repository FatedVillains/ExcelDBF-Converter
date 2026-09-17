namespace ExcelDbfConverter.Core.Enums;

/// <summary>DBF 字符编码选择。</summary>
public enum DbfEncodingKind
{
    Auto,
    Gbk,
    Gb2312,
    Utf8,
    Ansi,
}

public static class DbfEncodingKindExtensions
{
    /// <summary>
    /// 返回对应 Windows 代码页。
    /// 0 = 系统默认 ANSI，-1 = 自动识别（需要运行时探测），65001 = UTF-8。
    /// </summary>
    public static int GetCodepage(this DbfEncodingKind kind) => kind switch
    {
        DbfEncodingKind.Gbk => 936,
        DbfEncodingKind.Gb2312 => 936,
        DbfEncodingKind.Utf8 => 65001,
        DbfEncodingKind.Ansi => 0,
        _ => -1,
    };

    public static string DisplayName(this DbfEncodingKind kind) => kind switch
    {
        DbfEncodingKind.Auto => "自动识别",
        DbfEncodingKind.Gbk => "GBK",
        DbfEncodingKind.Gb2312 => "GB2312",
        DbfEncodingKind.Utf8 => "UTF-8",
        DbfEncodingKind.Ansi => "ANSI",
        _ => kind.ToString(),
    };
}
