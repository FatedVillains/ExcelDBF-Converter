using System.Text.RegularExpressions;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Application.Services;

/// <summary>DBF 字段名校验器。</summary>
public sealed partial class FieldNameValidator : IFieldNameValidator
{
    public ValidationResult Validate(string name, IEnumerable<string> existingNames)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return ValidationResult.Error("字段名称不能为空。");
        }

        if (trimmed.Length > Core.Constants.DbfConstants.MaxFieldNameLength)
        {
            return ValidationResult.Error($"字段名称长度不能超过 {Core.Constants.DbfConstants.MaxFieldNameLength} 个字符。");
        }

        if (char.IsDigit(trimmed[0]))
        {
            return ValidationResult.Error("字段名称不能以数字开头。");
        }

        if (!ValidNameRegex().IsMatch(trimmed))
        {
            return ValidationResult.Error("字段名称只能包含字母、数字和下划线，不能包含空格或特殊字符。");
        }

        if (existingNames.Any(n => string.Equals(n, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return ValidationResult.Error("DBF 字段名称重复。");
        }

        if (trimmed.Any(c => c > 127))
        {
            return ValidationResult.Warning("字段名称包含非英文字符，建议使用英文或拼音字段名称。");
        }

        return ValidationResult.Ok();
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex ValidNameRegex();
}
