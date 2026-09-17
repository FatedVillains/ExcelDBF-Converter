using System.Text;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Infrastructure.Dbf;

/// <summary>DBF 编码解析与语言驱动字节映射。</summary>
public static class DbfEncodingHelper
{
    private static bool _registered;

    private static void EnsureRegistered()
    {
        if (_registered) return;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _registered = true;
    }

    /// <summary>根据语言驱动字节返回 Windows 代码页；未知返回 0。</summary>
    public static int GetCodepage(byte languageDriver) => languageDriver switch
    {
        0x01 => 437,
        0x02 => 850,
        0x03 => 1252,
        0x57 or 0x58 or 0x59 => 1252,
        0x64 => 852,
        0x65 => 866,
        0x66 => 865,
        0x67 => 861,
        0x68 => 895,
        0x69 => 620,
        0x6A => 737,
        0x6B => 857,
        0x78 => 950,
        0x79 => 949,
        0x7A => 936,
        0x7B => 932,
        0x7C => 874,
        0x7D => 1255,
        0x7E => 1256,
        0x96 => 10007,
        0x97 => 10029,
        0x98 => 10006,
        0xC8 => 1250,
        0xC9 => 1251,
        0xCA => 1254,
        0xCB => 1253,
        _ => 0,
    };

    /// <summary>根据代码页返回语言驱动字节；未知返回 0。</summary>
    public static byte GetLanguageDriverByte(int codepage) => codepage switch
    {
        437 => 0x01,
        850 => 0x02,
        1252 => 0x03,
        852 => 0x64,
        866 => 0x65,
        865 => 0x66,
        861 => 0x67,
        737 => 0x6A,
        857 => 0x6B,
        950 => 0x78,
        949 => 0x79,
        936 => 0x7A,
        932 => 0x7B,
        874 => 0x7C,
        1255 => 0x7D,
        1256 => 0x7E,
        1250 => 0xC8,
        1251 => 0xC9,
        1254 => 0xCA,
        1253 => 0xCB,
        _ => 0,
    };

    /// <summary>根据写入编码返回语言驱动字节。</summary>
    public static byte GetLanguageDriverByteForWrite(DbfEncodingKind kind)
    {
        int cp = kind.GetCodepage();
        if (cp == 936) return 0x7A;
        if (cp == 65001) return 0x00;
        if (cp == 0) return GetLanguageDriverByte(System.Text.Encoding.Default?.CodePage == 0 ? 936 : 936);
        return GetLanguageDriverByte(cp);
    }

    /// <summary>根据显式编码返回 Encoding。</summary>
    public static Encoding GetEncoding(DbfEncodingKind kind)
    {
        EnsureRegistered();
        int cp = kind.GetCodepage();
        return cp switch
        {
            936 => Encoding.GetEncoding(936),
            65001 => new UTF8Encoding(false),
            0 => Encoding.GetEncoding(936),
            _ => Encoding.GetEncoding(cp),
        };
    }

    /// <summary>
    /// 解析实际使用的编码：
    /// Auto 时优先使用语言驱动字节，无标识则用 UTF-8 严格解码启发式，否则回退 GBK。
    /// </summary>
    public static Encoding ResolveEncoding(DbfEncodingKind kind, byte languageDriver)
    {
        EnsureRegistered();
        if (kind != DbfEncodingKind.Auto)
        {
            return GetEncoding(kind);
        }

        int cp = GetCodepage(languageDriver);
        if (cp > 0)
        {
            return Encoding.GetEncoding(cp);
        }

        return Encoding.GetEncoding(936);
    }
}
