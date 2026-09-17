using System.Windows;
using System.Windows.Threading;
using ExcelDbfConverter.Application;
using ExcelDbfConverter.Desktop.Abstractions;
using ExcelDbfConverter.Desktop.ViewModels;
using ExcelDbfConverter.Infrastructure;
using ExcelDbfConverter.Infrastructure.Configuration;
using ExcelDbfConverter.Infrastructure.SQLite;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ExcelDbfConverter.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var paths = new AppPaths();
        paths.EnsureDirectories();

        Log.Logger = Infrastructure.Logging.LogConfiguration.CreateLogger(paths.LogsDirectory);

        var services = new ServiceCollection();
        services.AddInfrastructure(paths);
        services.AddApplication();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<ExcelToDbfViewModel>();
        services.AddSingleton<DbfToExcelViewModel>();
        services.AddSingleton<BatchConversionViewModel>();
        services.AddSingleton<TemplateManagementViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        Log.Information("程序启动。");

        try
        {
            _serviceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库初始化失败。");
        }

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("程序退出。");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "未处理异常。");
        MessageBox.Show($"程序发生错误：{e.Exception.Message}\n详细信息请查看日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
