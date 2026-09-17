namespace ExcelDbfConverter.Core.Enums;

/// <summary>Visual FoxPro DBF 字段类型。</summary>
public enum DbfFieldType
{
    /// <summary>C 字符型</summary>
    Character,
    /// <summary>N 数值型</summary>
    Numeric,
    /// <summary>F 浮点型</summary>
    Float,
    /// <summary>D 日期型</summary>
    Date,
    /// <summary>T 日期时间型</summary>
    DateTime,
    /// <summary>L 逻辑型</summary>
    Logical,
    /// <summary>I 整型</summary>
    Integer,
    /// <summary>B 双精度型</summary>
    Double,
    /// <summary>M 备注型（第一版不读取 FPT，值返回 null）</summary>
    Memo,
    /// <summary>其它不支持类型（G/P/Y/V 等，值返回 null）</summary>
    Unsupported,
}

public static class DbfFieldTypeExtensions
{
    /// <summary>返回 DBF 文件中的单字符类型标识。</summary>
    public static char ToDbfCode(this DbfFieldType type) => type switch
    {
        DbfFieldType.Character => 'C',
        DbfFieldType.Numeric => 'N',
        DbfFieldType.Float => 'F',
        DbfFieldType.Date => 'D',
        DbfFieldType.DateTime => 'T',
        DbfFieldType.Logical => 'L',
        DbfFieldType.Integer => 'I',
        DbfFieldType.Double => 'B',
        DbfFieldType.Memo => 'M',
        DbfFieldType.Unsupported => 'C',
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    /// <summary>从 DBF 单字符类型标识解析字段类型；无法识别时返回 null。</summary>
    public static DbfFieldType? FromDbfCode(char code) => char.ToUpperInvariant(code) switch
    {
        'C' => DbfFieldType.Character,
        'N' => DbfFieldType.Numeric,
        'F' => DbfFieldType.Float,
        'D' => DbfFieldType.Date,
        'T' => DbfFieldType.DateTime,
        'L' => DbfFieldType.Logical,
        'I' => DbfFieldType.Integer,
        'B' => DbfFieldType.Double,
        'M' => DbfFieldType.Memo,
        'G' => DbfFieldType.Unsupported,
        'P' => DbfFieldType.Unsupported,
        'Y' => DbfFieldType.Unsupported,
        'V' => DbfFieldType.Unsupported,
        'Q' => DbfFieldType.Unsupported,
        'W' => DbfFieldType.Unsupported,
        _ => null,
    };

    /// <summary>是否为 Excel → DBF 方向支持、可供用户选择的类型。</summary>
    public static bool IsSupportedForExport(this DbfFieldType type) =>
        type is DbfFieldType.Character or DbfFieldType.Numeric or DbfFieldType.Float
            or DbfFieldType.Date or DbfFieldType.DateTime or DbfFieldType.Logical
            or DbfFieldType.Integer or DbfFieldType.Double;

    /// <summary>是否具有可配置的字段长度。</summary>
    public static bool HasLength(this DbfFieldType type) =>
        type is DbfFieldType.Character or DbfFieldType.Numeric or DbfFieldType.Float;

    /// <summary>是否具有小数位配置。</summary>
    public static bool HasDecimals(this DbfFieldType type) =>
        type is DbfFieldType.Numeric or DbfFieldType.Float;

    /// <summary>各类型的固定存储长度。</summary>
    public static int DefaultLength(this DbfFieldType type) => type switch
    {
        DbfFieldType.Character => 10,
        DbfFieldType.Numeric => 10,
        DbfFieldType.Float => 10,
        DbfFieldType.Date => 8,
        DbfFieldType.DateTime => 8,
        DbfFieldType.Logical => 1,
        DbfFieldType.Integer => 4,
        DbfFieldType.Double => 8,
        DbfFieldType.Memo => 10,
        _ => 0,
    };

    /// <summary>UI 显示名称。</summary>
    public static string DisplayName(this DbfFieldType type) => type switch
    {
        DbfFieldType.Character => "字符型 (C)",
        DbfFieldType.Numeric => "数值型 (N)",
        DbfFieldType.Float => "浮点型 (F)",
        DbfFieldType.Date => "日期型 (D)",
        DbfFieldType.DateTime => "日期时间型 (T)",
        DbfFieldType.Logical => "逻辑型 (L)",
        DbfFieldType.Integer => "整型 (I)",
        DbfFieldType.Double => "双精度型 (B)",
        DbfFieldType.Memo => "备注型 (M)",
        _ => "不支持类型",
    };
}
