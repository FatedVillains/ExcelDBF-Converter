using ExcelDbfConverter.Core.Enums;

namespace ExcelDbfConverter.Core.DTOs;

/// <summary>Excel → DBF 转换请求。</summary>
public sealed class ExcelToDbfRequest
{
    /// <summary>源 Excel 文件路径。</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>选择的 Sheet 名称。</summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>输出 DBF 文件路径。</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>字段映射配置。</summary>
    public List<Models.FieldMapping> FieldMappings { get; set; } = new();

    /// <summary>是否首行为表头。</summary>
    public bool HasHeaderRow { get; set; } = true;

    /// <summary>DBF 输出编码。</summary>
    public DbfEncodingKind Encoding { get; set; } = DbfEncodingKind.Gbk;

    /// <summary>错误处理方式。</summary>
    public ErrorHandlingMode ErrorHandling { get; set; } = ErrorHandlingMode.Stop;
}
