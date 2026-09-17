using ExcelDbfConverter.Core.Enums;

namespace ExcelDbfConverter.Core.DTOs;

/// <summary>DBF → Excel 转换请求。</summary>
public sealed class DbfToExcelRequest
{
    /// <summary>源 DBF 文件路径。</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>输出 Excel 文件路径。</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>Excel 输出格式。</summary>
    public ExcelFormat ExcelFormat { get; set; } = ExcelFormat.Xlsx;

    /// <summary>Sheet 名称。</summary>
    public string SheetName { get; set; } = "数据导出";

    /// <summary>每个 Sheet 最大数据行数（拆分阈值）。</summary>
    public int MaxRowsPerSheet { get; set; } = 500_000;

    /// <summary>DBF 读取编码。</summary>
    public DbfEncodingKind Encoding { get; set; } = DbfEncodingKind.Auto;

    /// <summary>是否包含已删除记录。</summary>
    public bool IncludeDeleted { get; set; }
}
