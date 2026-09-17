using ExcelDbfConverter.Core.Enums;

namespace ExcelDbfConverter.Core.Models;

/// <summary>转换历史记录。</summary>
public sealed class ConversionHistoryEntry
{
    public int Id { get; set; }

    public ConversionType ConversionType { get; set; }

    public string SourceFileName { get; set; } = string.Empty;

    public string SourceFilePath { get; set; } = string.Empty;

    public string TargetFileName { get; set; } = string.Empty;

    public string TargetFilePath { get; set; } = string.Empty;

    public ConversionStatus Status { get; set; }

    public long TotalRows { get; set; }

    public long SuccessRows { get; set; }

    public long FailedRows { get; set; }

    public long ElapsedMilliseconds { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.Now;
}
