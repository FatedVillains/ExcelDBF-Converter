using ExcelDbfConverter.Core.DTOs;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>转换任务接口。</summary>
public interface IConversionTask<in TRequest>
{
    Task<ConversionResult> ExecuteAsync(
        TRequest request,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken);
}
