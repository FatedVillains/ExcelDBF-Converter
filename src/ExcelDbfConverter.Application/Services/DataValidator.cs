using System.Globalization;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Application.Services;

/// <summary>数据值校验与转换器。</summary>
public sealed class DataValidator : IDataValidator
{
    public bool TryConvert(object? value, FieldMapping field, out object? converted, out string? error)
    {
        converted = null;
        error = null;

        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
        {
            // 空值始终允许，映射为目标类型的 null。
            return true;
        }

        switch (field.DbfFieldType)
        {
            case DbfFieldType.Character:
                converted = value.ToString();
                return true;

            case DbfFieldType.Numeric:
            case DbfFieldType.Float:
                if (TryConvertDecimal(value, out var dec))
                {
                    if (dec.ToString(CultureInfo.InvariantCulture).Replace("-", "").Replace(".", "").Length > field.Length)
                    {
                        error = "数据长度超过字段限制。";
                        return false;
                    }
                    converted = dec;
                    return true;
                }
                error = "无法转换为数字。";
                return false;

            case DbfFieldType.Date:
                if (TryConvertDate(value, out var date))
                {
                    converted = date.Date;
                    return true;
                }
                error = "日期格式错误。";
                return false;

            case DbfFieldType.DateTime:
                if (TryConvertDateTime(value, out var dateTime))
                {
                    converted = dateTime;
                    return true;
                }
                error = "日期时间格式错误。";
                return false;

            case DbfFieldType.Logical:
                if (TryConvertLogical(value, out var logical))
                {
                    converted = logical;
                    return true;
                }
                error = "无法识别为逻辑值。";
                return false;

            case DbfFieldType.Integer:
                if (TryConvertInteger(value, out var integer))
                {
                    converted = integer;
                    return true;
                }
                error = "无法转换为整数。";
                return false;

            case DbfFieldType.Double:
                if (TryConvertDouble(value, out var dbl))
                {
                    converted = dbl;
                    return true;
                }
                error = "无法转换为数字。";
                return false;

            default:
                converted = value.ToString();
                return true;
        }
    }

    private static bool TryConvertDecimal(object? value, out decimal result)
    {
        if (value is decimal d)
        {
            result = d;
            return true;
        }
        if (value is double dbl)
        {
            result = (decimal)dbl;
            return true;
        }
        if (value is int i)
        {
            result = i;
            return true;
        }
        if (value is long l)
        {
            result = l;
            return true;
        }
        if (value is string s)
        {
            return decimal.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
        result = 0;
        return false;
    }

    private static bool TryConvertInteger(object? value, out int result)
    {
        if (value is int i)
        {
            result = i;
            return true;
        }
        if (value is long l && l is >= int.MinValue and <= int.MaxValue)
        {
            result = (int)l;
            return true;
        }
        if (value is double dbl && dbl == Math.Floor(dbl) && dbl is >= int.MinValue and <= int.MaxValue)
        {
            result = (int)dbl;
            return true;
        }
        if (value is string s)
        {
            return int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
        result = 0;
        return false;
    }

    private static bool TryConvertDouble(object? value, out double result)
    {
        if (value is double dbl)
        {
            result = dbl;
            return true;
        }
        if (value is decimal d)
        {
            result = (double)d;
            return true;
        }
        if (value is int i)
        {
            result = i;
            return true;
        }
        if (value is string s)
        {
            return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
        result = 0;
        return false;
    }

    private static bool TryConvertDate(object? value, out DateTime result)
    {
        if (value is DateTime dt)
        {
            result = dt.Date;
            return true;
        }
        if (value is string s)
        {
            return DateTime.TryParseExact(s.Trim(), new[] { "yyyy-MM-dd", "yyyy/M/d", "yyyy年M月d日" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
        result = default;
        return false;
    }

    private static bool TryConvertDateTime(object? value, out DateTime result)
    {
        if (value is DateTime dt)
        {
            result = dt;
            return true;
        }
        if (value is string s)
        {
            return DateTime.TryParse(s.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
        result = default;
        return false;
    }

    private static bool TryConvertLogical(object? value, out bool result)
    {
        if (value is bool b)
        {
            result = b;
            return true;
        }
        if (value is string s)
        {
            var t = s.Trim().ToLowerInvariant();
            if (Core.Constants.AppDefaults.TrueValues.Contains(t))
            {
                result = true;
                return true;
            }
            if (Core.Constants.AppDefaults.FalseValues.Contains(t))
            {
                result = false;
                return true;
            }
        }
        if (value is double dbl)
        {
            if (dbl == 1)
            {
                result = true;
                return true;
            }
            if (dbl == 0)
            {
                result = false;
                return true;
            }
        }
        result = false;
        return false;
    }
}
