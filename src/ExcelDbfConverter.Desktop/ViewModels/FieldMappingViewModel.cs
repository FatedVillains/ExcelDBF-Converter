using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Desktop.ViewModels;

/// <summary>字段映射的可编辑包装。</summary>
public sealed partial class FieldMappingViewModel : ObservableObject
{
    [ObservableProperty]
    private string _excelColumnName = string.Empty;

    [ObservableProperty]
    private string _dbfFieldName = string.Empty;

    [ObservableProperty]
    private DbfFieldType _dbfFieldType = DbfFieldType.Character;

    [ObservableProperty]
    private int _length = 10;

    [ObservableProperty]
    private int _decimalCount;

    [ObservableProperty]
    private bool _isExport = true;

    public int ExcelColumnIndex { get; set; } = -1;

    public bool HasLength => DbfFieldType.HasLength();

    public bool HasDecimals => DbfFieldType.HasDecimals();

    public static IReadOnlyList<DbfFieldType> SupportedTypes { get; } = new List<DbfFieldType>
    {
        DbfFieldType.Character,
        DbfFieldType.Numeric,
        DbfFieldType.Float,
        DbfFieldType.Date,
        DbfFieldType.DateTime,
        DbfFieldType.Logical,
        DbfFieldType.Integer,
        DbfFieldType.Double,
    };

    public static FieldMappingViewModel FromMapping(FieldMapping mapping) => new()
    {
        ExcelColumnName = mapping.ExcelColumnName,
        ExcelColumnIndex = mapping.ExcelColumnIndex,
        DbfFieldName = mapping.DbfFieldName,
        DbfFieldType = mapping.DbfFieldType,
        Length = mapping.Length,
        DecimalCount = mapping.DecimalCount,
        IsExport = mapping.IsExport,
    };

    public FieldMapping ToMapping() => new()
    {
        ExcelColumnName = ExcelColumnName,
        ExcelColumnIndex = ExcelColumnIndex,
        DbfFieldName = DbfFieldName,
        DbfFieldType = DbfFieldType,
        Length = Length,
        DecimalCount = DecimalCount,
        IsExport = IsExport,
    };

    partial void OnDbfFieldTypeChanged(DbfFieldType value)
    {
        OnPropertyChanged(nameof(HasLength));
        OnPropertyChanged(nameof(HasDecimals));
    }
}
