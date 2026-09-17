namespace ExcelDbfConverter.Core.Models;

/// <summary>转换模板。</summary>
public sealed class TemplateInfo
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0";

    public DateTime CreatedTime { get; set; } = DateTime.Now;

    public DateTime UpdatedTime { get; set; } = DateTime.Now;

    public List<TemplateFieldInfo> Fields { get; set; } = new();
}
