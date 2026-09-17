using ExcelDbfConverter.Core.Enums;

namespace ExcelDbfConverter.Core.Models;

/// <summary>应用设置。</summary>
public sealed class AppSettings
{
    /// <summary>默认输出目录。</summary>
    public string DefaultOutputDirectory { get; set; } = string.Empty;

    /// <summary>默认 Excel 输出格式。</summary>
    public ExcelFormat DefaultExcelFormat { get; set; } = ExcelFormat.Xlsx;

    /// <summary>默认 DBF 编码。</summary>
    public DbfEncodingKind DefaultDbfEncoding { get; set; } = DbfEncodingKind.Auto;

    /// <summary>数据预览行数。</summary>
    public int PreviewRowCount { get; set; } = 100;

    /// <summary>自动识别为文本类型的关键词。</summary>
    public List<string> TextKeywords { get; set; } = new();
}
