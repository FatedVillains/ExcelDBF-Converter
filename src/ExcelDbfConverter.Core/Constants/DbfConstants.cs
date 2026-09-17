namespace ExcelDbfConverter.Core.Constants;

/// <summary>Visual FoxPro DBF 格式常量。</summary>
public static class DbfConstants
{
    /// <summary>DBF 字段名最大长度。</summary>
    public const int MaxFieldNameLength = 10;

    /// <summary>字符型字段最大长度（FoxPro 限制）。</summary>
    public const int MaxCharacterLength = 254;

    /// <summary>数值型/浮点型字段最大长度（FoxPro 限制）。</summary>
    public const int MaxNumericLength = 20;

    /// <summary>字段描述符固定字节数。</summary>
    public const int FieldDescriptorSize = 32;

    /// <summary>文件头固定前缀字节数（不含字段描述符）。</summary>
    public const int HeaderPrefixSize = 32;

    /// <summary>字段描述符结束后的 0x0D 终止符占 1 字节。</summary>
    public const int HeaderTerminatorSize = 1;

    /// <summary>Visual FoxPro 表头在字段描述符之后的 263 字节 backlink 区。</summary>
    public const int HeaderBacklinkSize = 263;

    /// <summary>Visual FoxPro 表头版本标识。</summary>
    public const byte VfpVersion = 0x30;

    /// <summary>dBase III / FoxPro 2.x 版本标识（读取兼容）。</summary>
    public const byte DBase3Version = 0x03;

    /// <summary>带 Memo 的 dBase III 版本标识。</summary>
    public const byte DBase3MemoVersion = 0x83;

    /// <summary>记录删除标记：未删除。</summary>
    public const byte RecordActive = 0x20;

    /// <summary>记录删除标记：已删除。</summary>
    public const byte RecordDeleted = 0x2A;

    /// <summary>文件结束标记 0x1A。</summary>
    public const byte EndOfFileMarker = 0x1A;

    /// <summary>语言驱动字节：GBK（代码页 936）。</summary>
    public const byte LanguageDriverGbk = 0x7A;

    /// <summary>语言驱动字节：Windows ANSI（代码页 1252）。</summary>
    public const byte LanguageDriverAnsi = 0x03;
}
