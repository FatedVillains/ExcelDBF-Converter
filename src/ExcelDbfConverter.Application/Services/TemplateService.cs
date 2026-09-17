using System.Text.Json;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Application.Services;

/// <summary>模板服务：CRUD + 导入导出 + 匹配应用。</summary>
public sealed class TemplateService : ITemplateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ITemplateRepository _repository;

    public TemplateService(ITemplateRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<TemplateInfo>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<TemplateInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public Task<int> SaveAsync(TemplateInfo template, CancellationToken cancellationToken = default) =>
        _repository.SaveAsync(template, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    public async Task<TemplateInfo> ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var dto = await JsonSerializer.DeserializeAsync<TemplateFileDto>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("模板文件内容为空或格式错误。");

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidDataException("模板缺少名称。");
        }

        var template = new TemplateInfo
        {
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Version = dto.Version ?? "1.0",
            Fields = (dto.Fields ?? new List<TemplateFieldDto>())
                .OrderBy(f => f.SortOrder)
                .Select((f, i) => new TemplateFieldInfo
                {
                    ExcelColumnName = f.ExcelColumnName ?? string.Empty,
                    DbfFieldName = f.DbfFieldName ?? string.Empty,
                    DbfFieldType = DbfFieldTypeExtensions.FromDbfCode(f.DbfFieldType?[0] ?? 'C') ?? DbfFieldType.Character,
                    FieldLength = f.Length,
                    DecimalCount = f.DecimalCount,
                    IsRequired = f.Required,
                    SortOrder = f.SortOrder > 0 ? f.SortOrder : i + 1,
                }).ToList(),
        };

        await SaveAsync(template, cancellationToken).ConfigureAwait(false);
        return template;
    }

    public async Task ExportAsync(TemplateInfo template, string filePath, CancellationToken cancellationToken = default)
    {
        var dto = new TemplateFileDto
        {
            Name = template.Name,
            Description = template.Description,
            Version = template.Version,
            Fields = template.Fields.OrderBy(f => f.SortOrder).Select(f => new TemplateFieldDto
            {
                ExcelColumnName = f.ExcelColumnName,
                DbfFieldName = f.DbfFieldName,
                DbfFieldType = f.DbfFieldType.ToDbfCode().ToString(),
                Length = f.FieldLength,
                DecimalCount = f.DecimalCount,
                Required = f.IsRequired,
                SortOrder = f.SortOrder,
            }).ToList(),
        };

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, dto, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public (List<FieldMapping> Mappings, List<string> MissingColumns, List<string> UnusedColumns) ApplyTemplate(
        TemplateInfo template,
        IReadOnlyList<FieldMapping> sourceMappings)
    {
        var missing = new List<string>();
        var unused = new List<string>();
        var result = new List<FieldMapping>();

        var byExcelName = sourceMappings
            .GroupBy(m => m.ExcelColumnName?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var usedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int order = 1;
        foreach (var field in template.Fields.OrderBy(f => f.SortOrder))
        {
            if (byExcelName.TryGetValue(field.ExcelColumnName, out var source))
            {
                result.Add(new FieldMapping
                {
                    ExcelColumnName = source.ExcelColumnName,
                    ExcelColumnIndex = source.ExcelColumnIndex,
                    DbfFieldName = field.DbfFieldName,
                    DbfFieldType = field.DbfFieldType,
                    Length = field.FieldLength,
                    DecimalCount = field.DecimalCount,
                    IsExport = true,
                });
                usedColumns.Add(source.ExcelColumnName);
            }
            else
            {
                // 模板字段在 Excel 中缺失：仍加入映射（导出列名存在但无数据来源）。
                missing.Add(field.ExcelColumnName);
                result.Add(new FieldMapping
                {
                    ExcelColumnName = field.ExcelColumnName,
                    ExcelColumnIndex = -1,
                    DbfFieldName = field.DbfFieldName,
                    DbfFieldType = field.DbfFieldType,
                    Length = field.FieldLength,
                    DecimalCount = field.DecimalCount,
                    IsExport = true,
                });
            }
            order++;
        }

        foreach (var source in sourceMappings)
        {
            if (!usedColumns.Contains(source.ExcelColumnName))
            {
                unused.Add(source.ExcelColumnName);
            }
        }

        return (result, missing, unused);
    }

    private sealed class TemplateFileDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public List<TemplateFieldDto>? Fields { get; set; }
    }

    private sealed class TemplateFieldDto
    {
        public string? ExcelColumnName { get; set; }
        public string? DbfFieldName { get; set; }
        public string? DbfFieldType { get; set; }
        public int Length { get; set; }
        public int DecimalCount { get; set; }
        public bool Required { get; set; }
        public int SortOrder { get; set; }
    }
}
