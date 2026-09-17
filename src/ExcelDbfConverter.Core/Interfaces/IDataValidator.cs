using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>数据值校验与转换器。</summary>
public interface IDataValidator
{
    /// <summary>
    /// 将源单元格值转换为 DBF 目标类型；失败时返回错误信息。
    /// </summary>
    bool TryConvert(object? value, FieldMapping field, out object? converted, out string? error);
}
