using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Infrastructure.Configuration;
using ExcelDbfConverter.Infrastructure.Dbf;
using ExcelDbfConverter.Infrastructure.Excel;
using ExcelDbfConverter.Infrastructure.SQLite;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelDbfConverter.Infrastructure;

/// <summary>Infrastructure 层依赖注入注册。</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, AppPaths? paths = null)
    {
        paths ??= new AppPaths();
        paths.EnsureDirectories();

        services.AddSingleton(paths);
        services.AddSingleton(new DatabaseInitializer(paths.DatabasePath));

        services.AddSingleton<IDbfReader, DbfReader>();
        services.AddTransient<IDbfWriter, DbfWriter>();
        services.AddSingleton<Func<IDbfWriter>>(sp => () => sp.GetRequiredService<IDbfWriter>());
        services.AddSingleton<IExcelReader, NpoiExcelReader>();
        services.AddSingleton<IExcelWriter, NpoiExcelWriter>();

        services.AddSingleton<ITemplateRepository, SqliteTemplateRepository>();
        services.AddSingleton<IHistoryRepository, SqliteHistoryRepository>();
        services.AddSingleton<ISettingsRepository, SqliteSettingsRepository>();

        return services;
    }
}
