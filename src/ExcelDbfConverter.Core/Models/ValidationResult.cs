namespace ExcelDbfConverter.Core.Models;

/// <summary>校验结果。</summary>
public sealed class ValidationResult
{
    public bool IsValid { get; set; } = true;

    /// <summary>错误/提示信息。</summary>
    public string? Message { get; set; }

    /// <summary>是否为建议（而非硬性错误）。</summary>
    public bool IsWarning { get; set; }

    public static ValidationResult Ok() => new();

    public static ValidationResult Error(string message) => new() { IsValid = false, Message = message };

    public static ValidationResult Warning(string message) => new() { IsValid = true, IsWarning = true, Message = message };
}
