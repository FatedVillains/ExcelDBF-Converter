namespace ExcelDbfConverter.Core.Enums;

/// <summary>转换结果状态。</summary>
public enum ConversionStatus
{
    Success,
    Failed,
    Cancelled,
    Partial,
}

public static class ConversionStatusExtensions
{
    public static string DisplayName(this ConversionStatus status) => status switch
    {
        ConversionStatus.Success => "成功",
        ConversionStatus.Failed => "失败",
        ConversionStatus.Cancelled => "已取消",
        ConversionStatus.Partial => "部分成功",
        _ => status.ToString(),
    };
}
