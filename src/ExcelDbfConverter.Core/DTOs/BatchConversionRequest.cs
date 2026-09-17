namespace ExcelDbfConverter.Core.DTOs;

/// <summary>批量转换请求。</summary>
public sealed class BatchConversionRequest
{
    /// <summary>转换方向。</summary>
    public Enums.ConversionType ConversionType { get; set; }

    /// <summary>源文件列表（多文件模式）。</summary>
    public List<string> SourceFiles { get; set; } = new();

    /// <summary>源目录（文件夹模式）。</summary>
    public string? SourceDirectory { get; set; }

    /// <summary>输出目录。</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>是否包含子目录。</summary>
    public bool IncludeSubdirectories { get; set; }

    /// <summary>重名处理方式。</summary>
    public Enums.OverwriteMode OverwriteMode { get; set; } = Enums.OverwriteMode.Rename;

    /// <summary>错误处理方式。</summary>
    public Enums.ErrorHandlingMode ErrorHandling { get; set; } = Enums.ErrorHandlingMode.Stop;

    /// <summary>Excel 输出格式（DBF→Excel 时使用）。</summary>
    public Enums.ExcelFormat ExcelFormat { get; set; } = Enums.ExcelFormat.Xlsx;

    /// <summary>DBF 编码。</summary>
    public Enums.DbfEncodingKind Encoding { get; set; } = Enums.DbfEncodingKind.Auto;
}
