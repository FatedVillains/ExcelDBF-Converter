using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>模板服务：CRUD + 导入导出 + 匹配应用。</summary>
public interface ITemplateService
{
    Task<IReadOnlyList<TemplateInfo>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TemplateInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> SaveAsync(TemplateInfo template, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>从 JSON 文件导入模板。</summary>
    Task<TemplateInfo> ImportAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>导出模板为 JSON 文件。</summary>
    Task ExportAsync(TemplateInfo template, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 Excel 列名精确匹配将模板应用到字段映射。
    /// 返回 (应用后的映射列表, 缺失的 Excel 字段, 未使用的 Excel 字段)。
    /// </summary>
    (List<FieldMapping> Mappings, List<string> MissingColumns, List<string> UnusedColumns) ApplyTemplate(
        TemplateInfo template,
        IReadOnlyList<FieldMapping> sourceMappings);
}
