using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Application.Services;

/// <summary>Excel 字段类型自动识别器。</summary>
public sealed class FieldTypeDetector : IFieldTypeDetector
{
    public IReadOnlyList<FieldMapping> Detect(
        IReadOnlyList<ExcelColumnInfo> columns,
        IReadOnlyList<ExcelRow> sampleRows,
        IReadOnlyCollection<string> textKeywords)
    {
        var mappings = new List<FieldMapping>();

        for (int colIndex = 0; colIndex < columns.Count; colIndex++)
        {
            var column = columns[colIndex];
            var mapping = new FieldMapping
            {
                ExcelColumnName = column.Name,
                ExcelColumnIndex = colIndex,
                IsExport = true,
            };

            // 强制文本：字段名包含关键词。
            if (ContainsKeyword(column.Name, textKeywords))
            {
                ApplyCharacter(mapping, sampleRows, colIndex);
                mappings.Add(mapping);
                continue;
            }

            var values = sampleRows
                .Select(r => colIndex < r.Values.Count ? r.Values[colIndex] : null)
                .Where(v => v is not null && !IsEmpty(v))
                .ToList();

            if (values.Count == 0)
            {
                ApplyCharacter(mapping, sampleRows, colIndex);
                mappings.Add(mapping);
                continue;
            }

            mapping.DbfFieldType = InferType(values);
            switch (mapping.DbfFieldType)
            {
                case DbfFieldType.Numeric:
                case DbfFieldType.Float:
                    ApplyNumeric(mapping, values);
                    break;
                case DbfFieldType.Date:
                    mapping.Length = 8;
                    mapping.DecimalCount = 0;
                    break;
                case DbfFieldType.DateTime:
                    mapping.Length = 8;
                    mapping.DecimalCount = 0;
                    break;
                case DbfFieldType.Logical:
                    mapping.Length = 1;
                    mapping.DecimalCount = 0;
                    break;
                default:
                    ApplyCharacter(mapping, sampleRows, colIndex);
                    break;
            }

            mapping.DbfFieldName = DefaultFieldName(column.Name, mappings);
            mappings.Add(mapping);
        }

        return mappings;
    }

    private static bool ContainsKeyword(string columnName, IReadOnlyCollection<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return false;
        }
        return keywords.Any(k => !string.IsNullOrWhiteSpace(k) && columnName.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsEmpty(object? value) =>
        value is string s && string.IsNullOrWhiteSpace(s);

    private static DbfFieldType InferType(IReadOnlyList<object?> values)
    {
        if (values.All(IsLogicalCandidate))
        {
            return DbfFieldType.Logical;
        }
        if (values.All(IsIntegerCandidate))
        {
            return DbfFieldType.Numeric;
        }
        if (values.All(IsNumericCandidate))
        {
            return DbfFieldType.Numeric;
        }
        if (values.All(IsDateCandidate))
        {
            return DbfFieldType.Date;
        }
        if (values.All(IsDateTimeCandidate))
        {
            return DbfFieldType.DateTime;
        }
        return DbfFieldType.Character;
    }

    private static bool IsLogicalCandidate(object? value)
    {
        if (value is bool)
        {
            return true;
        }
        if (value is string s)
        {
            var t = s.Trim().ToLowerInvariant();
            return t is "true" or "false" or "是" or "否" or "y" or "n" or "1" or "0" or "t" or "f";
        }
        if (value is double d && (d == 0 || d == 1))
        {
            return true;
        }
        return false;
    }

    private static bool IsIntegerCandidate(object? value)
    {
        if (value is double d)
        {
            return d == Math.Floor(d);
        }
        if (value is string s)
        {
            return int.TryParse(s.Trim(), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out _);
        }
        return false;
    }

    private static bool IsNumericCandidate(object? value)
    {
        if (value is double or decimal or int or long or float)
        {
            return true;
        }
        if (value is string s)
        {
            return double.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _);
        }
        return false;
    }

    private static bool IsDateCandidate(object? value)
    {
        if (value is DateTime dt)
        {
            return dt.TimeOfDay == TimeSpan.Zero;
        }
        if (value is string s)
        {
            return DateOnly.TryParse(s.Trim(), out _);
        }
        return false;
    }

    private static bool IsDateTimeCandidate(object? value)
    {
        if (value is DateTime)
        {
            return true;
        }
        if (value is string s)
        {
            return DateTime.TryParse(s.Trim(), out _);
        }
        return false;
    }

    private static void ApplyCharacter(FieldMapping mapping, IReadOnlyList<ExcelRow> sampleRows, int colIndex)
    {
        mapping.DbfFieldType = DbfFieldType.Character;
        int maxLength = 0;
        foreach (var row in sampleRows)
        {
            if (colIndex < row.Values.Count && row.Values[colIndex] is string s)
            {
                maxLength = Math.Max(maxLength, s.Length);
            }
        }
        mapping.Length = Math.Clamp(maxLength + Core.Constants.AppDefaults.CharacterLengthBuffer, 1, Core.Constants.DbfConstants.MaxCharacterLength);
        mapping.DecimalCount = 0;
    }

    private static void ApplyNumeric(FieldMapping mapping, IReadOnlyList<object?> values)
    {
        int integerDigits = 0;
        int decimalDigits = 0;

        foreach (var value in values)
        {
            string text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            if (decimal.TryParse(text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var dec))
            {
                int dot = text.IndexOf('.');
                int intPart = dot < 0 ? text.Length : dot;
                if (text.StartsWith('-'))
                {
                    intPart--;
                }
                integerDigits = Math.Max(integerDigits, intPart);
                if (dot >= 0)
                {
                    decimalDigits = Math.Max(decimalDigits, text.Length - dot - 1);
                }
            }
        }

        integerDigits = Math.Max(integerDigits, 1);
        decimalDigits = Math.Clamp(decimalDigits, 0, 18);
        int length = integerDigits + (decimalDigits > 0 ? decimalDigits + 1 : 0);
        length = Math.Clamp(length, 1, Core.Constants.DbfConstants.MaxNumericLength);

        mapping.DbfFieldType = decimalDigits > 0 ? DbfFieldType.Numeric : DbfFieldType.Numeric;
        mapping.Length = length;
        mapping.DecimalCount = decimalDigits;
    }

    private static string DefaultFieldName(string excelName, IReadOnlyList<FieldMapping> existing)
    {
        string baseName = ToDbfName(excelName);
        if (string.IsNullOrEmpty(baseName))
        {
            baseName = "F";
        }
        string candidate = baseName;
        int suffix = 1;
        while (existing.Any(m => string.Equals(m.DbfFieldName, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = baseName.Length >= 9 ? baseName[..9] + suffix.ToString() : baseName + suffix.ToString();
            suffix++;
        }
        return candidate;
    }

    private static string ToDbfName(string excelName)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in excelName ?? string.Empty)
        {
            if (char.IsAsciiLetterOrDigit(c) || c == '_')
            {
                sb.Append(char.ToUpperInvariant(c));
            }
            else
            {
                sb.Append('_');
            }
            if (sb.Length >= 10)
            {
                break;
            }
        }

        string name = sb.ToString().Trim('_');
        if (name.Length > 0 && char.IsDigit(name[0]))
        {
            name = "F" + name;
        }
        return name.Length > 10 ? name[..10] : name;
    }
}
