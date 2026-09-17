namespace ExcelDbfConverter.Core.Enums;

/// <summary>批量转换时目标文件重名处理方式。</summary>
public enum OverwriteMode
{
    Overwrite,
    Rename,
    Skip,
}

public static class OverwriteModeExtensions
{
    public static string DisplayName(this OverwriteMode mode) => mode switch
    {
        OverwriteMode.Overwrite => "覆盖",
        OverwriteMode.Rename => "自动重命名",
        OverwriteMode.Skip => "跳过",
        _ => mode.ToString(),
    };
}
