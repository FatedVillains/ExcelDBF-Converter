namespace ExcelDbfConverter.Core.Models;

/// <summary>DBF 表信息（结构与元数据）。</summary>
public sealed class DbfTableInfo
{
    public string FilePath { get; set; } = string.Empty;

    public DbfTableSchema Schema { get; set; } = new();

    /// <summary>记录数（从表头读取）。</summary>
    public int RecordCount { get; set; }

    /// <summary>版本字节。</summary>
    public byte Version { get; set; }

    /// <summary>语言驱动字节（offset 29）。</summary>
    public byte LanguageDriver { get; set; }

    /// <summary>解析出的 Windows 代码页。</summary>
    public int Codepage { get; set; }

    /// <summary>是否包含 Memo 字段（第一版不处理 FPT）。</summary>
    public bool HasMemoField { get; set; }
}
