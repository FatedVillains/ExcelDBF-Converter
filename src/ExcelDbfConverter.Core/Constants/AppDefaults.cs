namespace ExcelDbfConverter.Core.Constants;

/// <summary>应用默认值。</summary>
public static class AppDefaults
{
    /// <summary>数据预览默认行数。</summary>
    public const int DefaultPreviewRows = 100;

    /// <summary>数据预览可选行数。</summary>
    public static readonly int[] PreviewRowOptions = { 100, 500, 1000 };

    /// <summary>字符型字段长度缓冲量（最大长度 + 缓冲）。</summary>
    public const int CharacterLengthBuffer = 5;

    /// <summary>类型识别抽样行数。</summary>
    public const int TypeDetectionSampleRows = 100;

    /// <summary>默认每个 Sheet 最大数据行数（DBF→Excel 拆分阈值）。</summary>
    public const int DefaultMaxRowsPerSheet = 500_000;

    /// <summary>首页最近转换记录显示条数。</summary>
    public const int RecentHistoryCount = 8;

    /// <summary>自动识别为文本类型的关键词默认值。</summary>
    public static readonly string[] DefaultTextKeywords =
    {
        "编号", "代码", "号码", "身份证", "电话", "手机", "学号", "考生号", "邮编", "账号", "卡号",
    };

    /// <summary>逻辑真值候选。</summary>
    public static readonly string[] TrueValues = { "true", "t", "y", "yes", "是", "1" };

    /// <summary>逻辑假值候选。</summary>
    public static readonly string[] FalseValues = { "false", "f", "n", "no", "否", "0" };
}
