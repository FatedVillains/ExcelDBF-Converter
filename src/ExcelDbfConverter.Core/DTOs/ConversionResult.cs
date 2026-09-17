using ExcelDbfConverter.Core.Enums;

namespace ExcelDbfConverter.Core.DTOs;

/// <summary>转换结果。</summary>
public sealed class ConversionResult
{
    public ConversionStatus Status { get; set; } = ConversionStatus.Success;

    /// <summary>输出文件路径。</summary>
    public string? OutputFilePath { get; set; }

    /// <summary>错误报告路径（如有）。</summary>
    public string? ErrorReportPath { get; set; }

    public long TotalRows { get; set; }

    public long SuccessRows { get; set; }

    public long FailedRows { get; set; }

    public TimeSpan Elapsed { get; set; }

    /// <summary>用户可见的失败信息。</summary>
    public string? Message { get; set; }

    public List<ConversionError> Errors { get; set; } = new();
}
