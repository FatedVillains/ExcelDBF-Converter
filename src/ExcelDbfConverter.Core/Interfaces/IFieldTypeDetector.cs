using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Core.Interfaces;

/// <summary>Excel 字段类型自动识别器。</summary>
public interface IFieldTypeDetector
{
    /// <summary>
    /// 根据列信息与样本数据识别字段映射。
    /// </summary>
    IReadOnlyList<FieldMapping> Detect(
        IReadOnlyList<ExcelColumnInfo> columns,
        IReadOnlyList<ExcelRow> sampleRows,
        IReadOnlyCollection<string> textKeywords);
}
