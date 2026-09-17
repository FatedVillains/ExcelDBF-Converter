using ExcelDbfConverter.Core.DTOs;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>转换服务：单文件与批量转换编排。</summary>
public interface IConversionService
{
    Task<ConversionResult> ConvertExcelToDbfAsync(
        ExcelToDbfRequest request,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ConversionResult> ConvertDbfToExcelAsync(
        DbfToExcelRequest request,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BatchItemResult>> BatchConvertAsync(
        BatchConversionRequest request,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
