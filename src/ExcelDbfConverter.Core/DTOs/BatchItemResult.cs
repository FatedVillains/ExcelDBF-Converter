using ExcelDbfConverter.Core.Enums;

namespace ExcelDbfConverter.Core.DTOs;

/// <summary>批量转换单文件结果。</summary>
public sealed class BatchItemResult
{
    public string FileName { get; set; } = string.Empty;

    public string SourcePath { get; set; } = string.Empty;

    public string? TargetPath { get; set; }

    public ConversionStatus Status { get; set; }

    public string Message { get; set; } = string.Empty;
}
