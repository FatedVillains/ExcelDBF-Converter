namespace ExcelDbfConverter.Core.DTOs;

/// <summary>转换进度信息。</summary>
public sealed class ConversionProgress
{
    public string CurrentFileName { get; set; } = string.Empty;

    public long CurrentRow { get; set; }

    public long TotalRows { get; set; }

    public double Percentage { get; set; }

    public TimeSpan Elapsed { get; set; }

    public TimeSpan? EstimatedRemaining { get; set; }
}
