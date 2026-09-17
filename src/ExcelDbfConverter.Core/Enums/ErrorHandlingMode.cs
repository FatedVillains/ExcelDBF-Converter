namespace ExcelDbfConverter.Core.Enums;

/// <summary>转换过程中遇到数据错误时的处理方式。</summary>
public enum ErrorHandlingMode
{
    /// <summary>立即停止转换。</summary>
    Stop,
    /// <summary>跳过错误行。</summary>
    SkipRow,
    /// <summary>错误字段置空。</summary>
    SetNull,
}

public static class ErrorHandlingModeExtensions
{
    public static string DisplayName(this ErrorHandlingMode mode) => mode switch
    {
        ErrorHandlingMode.Stop => "立即停止转换",
        ErrorHandlingMode.SkipRow => "跳过错误行",
        ErrorHandlingMode.SetNull => "错误字段置空",
        _ => mode.ToString(),
    };
}
