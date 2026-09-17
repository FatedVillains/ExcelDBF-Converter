namespace ExcelDbfConverter.Infrastructure.Configuration;

/// <summary>应用目录布局（绿色免安装：数据、模板、日志、配置均位于程序目录下）。</summary>
public sealed class AppPaths
{
    public AppPaths(string? baseDirectory = null)
    {
        BaseDirectory = baseDirectory ?? AppContext.BaseDirectory;
        DataDirectory = Path.Combine(BaseDirectory, "data");
        TemplatesDirectory = Path.Combine(BaseDirectory, "templates");
        LogsDirectory = Path.Combine(BaseDirectory, "logs");
        ConfigDirectory = Path.Combine(BaseDirectory, "config");
        DatabasePath = Path.Combine(DataDirectory, "ExcelDBFConverter.db");
    }

    public string BaseDirectory { get; }

    public string DataDirectory { get; }

    public string TemplatesDirectory { get; }

    public string LogsDirectory { get; }

    public string ConfigDirectory { get; }

    public string DatabasePath { get; }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(TemplatesDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ConfigDirectory);
    }
}
