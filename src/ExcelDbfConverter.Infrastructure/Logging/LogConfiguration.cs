using Serilog;
using Serilog.Events;

namespace ExcelDbfConverter.Infrastructure.Logging;

/// <summary>Serilog 日志初始化。</summary>
public static class LogConfiguration
{
    /// <summary>创建写入 logs 目录的日滚动文件日志。</summary>
    public static ILogger CreateLogger(string logsDirectory)
    {
        Directory.CreateDirectory(logsDirectory);
        string logPath = Path.Combine(logsDirectory, "app-.log");

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
