using ExcelDbfConverter.Application.Converters;
using ExcelDbfConverter.Application.Services;
using ExcelDbfConverter.Core.DTOs;
using ExcelDbfConverter.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelDbfConverter.Application;

/// <summary>Application 层依赖注入注册。</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IFieldTypeDetector, FieldTypeDetector>();
        services.AddSingleton<IFieldNameValidator, FieldNameValidator>();
        services.AddSingleton<IDataValidator, DataValidator>();

        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        services.AddSingleton<IConversionTask<ExcelToDbfRequest>, ExcelToDbfConversionTask>();
        services.AddSingleton<IConversionTask<DbfToExcelRequest>, DbfToExcelConversionTask>();
        services.AddSingleton<IConversionService, ConversionService>();

        return services;
    }
}
