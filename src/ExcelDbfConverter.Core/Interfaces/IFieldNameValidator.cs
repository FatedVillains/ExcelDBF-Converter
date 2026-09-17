using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>DBF 字段名校验器。</summary>
public interface IFieldNameValidator
{
    /// <summary>校验单个字段名；existingNames 用于重复检查。</summary>
    ValidationResult Validate(string name, IEnumerable<string> existingNames);
}
