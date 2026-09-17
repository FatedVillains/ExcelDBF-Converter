using System.Runtime.CompilerServices;
using System.Text;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Infrastructure.Dbf;

/// <summary>Visual FoxPro / dBASE DBF 读取器。</summary>
public sealed class DbfReader : IDbfReader
{
    public async Task<DbfTableInfo> ReadStructureAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = OpenReadStream(filePath);
        var header = await ReadHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
        return BuildTableInfo(filePath, header);
    }

    public async IAsyncEnumerable<DbfRecord> ReadRecordsAsync(
        string filePath,
        DbfReadOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = OpenReadStream(filePath);
        var header = await ReadHeaderAsync(stream, cancellationToken).ConfigureAwait(false);

        var encoding = DbfEncodingHelper.ResolveEncoding(options.Encoding, header.LanguageDriver);
        int recordLength = header.RecordLength;
        long dataOffset = header.HeaderLength;
        stream.Seek(dataOffset, SeekOrigin.Begin);

        var buffer = new byte[recordLength];
        int readCount = 0;
        int maxRecords = options.MaxRecords ?? int.MaxValue;

        for (int i = 0; i < header.RecordCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (readCount >= maxRecords)
            {
                yield break;
            }

            int bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                yield break;
            }

            if (buffer[0] == Core.Constants.DbfConstants.EndOfFileMarker)
            {
                yield break;
            }

            bool deleted = buffer[0] == Core.Constants.DbfConstants.RecordDeleted;
            if (deleted && !options.IncludeDeleted)
            {
                continue;
            }

            readCount++;
            var record = new DbfRecord { IsDeleted = deleted };

            foreach (var field in header.Fields)
            {
                record.Values.Add(DecodeField(buffer, field, encoding));
            }

            yield return record;
        }
    }

    private static FileStream OpenReadStream(string filePath)
    {
        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    private static async Task<DbfHeaderInfo> ReadHeaderAsync(Stream stream, CancellationToken cancellationToken)
    {
        var prefix = new byte[32];
        int read = await stream.ReadAsync(prefix, cancellationToken).ConfigureAwait(false);
        if (read < 32)
        {
            throw new InvalidDataException("DBF 文件头不完整。");
        }

        byte version = prefix[0];
        uint recordCount = BitConverter.ToUInt32(prefix, 4);
        ushort headerLength = BitConverter.ToUInt16(prefix, 8);
        ushort recordLength = BitConverter.ToUInt16(prefix, 10);
        byte languageDriver = prefix[29];

        if (headerLength < 33 || headerLength > 65535)
        {
            throw new InvalidDataException("DBF 文件头长度非法。");
        }

        // 读取字段描述符区域。
        int descriptorBytes = headerLength - 32;
        var descriptorArea = new byte[descriptorBytes];
        int descriptorRead = await stream.ReadAsync(descriptorArea, cancellationToken).ConfigureAwait(false);
        if (descriptorRead < descriptorBytes)
        {
            throw new InvalidDataException("DBF 字段描述符不完整。");
        }

        var fields = new List<DbfFieldDefinition>();
        int position = 0;
        while (position + 32 <= descriptorArea.Length)
        {
            // 0x0D 是字段描述符终止符。
            if (descriptorArea[position] == 0x0D)
            {
                break;
            }

            var field = new DbfFieldDefinition
            {
                Name = ReadFieldName(descriptorArea, position),
                Type = DbfFieldTypeExtensions.FromDbfCode((char)descriptorArea[position + 11])
                       ?? DbfFieldType.Unsupported,
                Length = descriptorArea[position + 16],
                DecimalCount = descriptorArea[position + 17],
            };

            fields.Add(field);
            position += 32;
        }

        // 计算字段在记录中的字节偏移（含 1 字节删除标记）。
        int offset = 1;
        foreach (var field in fields)
        {
            field.OffsetInRecord = offset;
            offset += field.Length;
        }

        return new DbfHeaderInfo
        {
            Version = version,
            RecordCount = (int)recordCount,
            HeaderLength = headerLength,
            RecordLength = recordLength,
            LanguageDriver = languageDriver,
            Fields = fields,
        };
    }

    private static string ReadFieldName(byte[] area, int position)
    {
        int end = position;
        while (end < position + 11 && area[end] != 0)
        {
            end++;
        }
        return Encoding.ASCII.GetString(area, position, end - position).Trim();
    }

    private static DbfTableInfo BuildTableInfo(string filePath, DbfHeaderInfo header)
    {
        var schema = new DbfTableSchema { Fields = header.Fields };
        return new DbfTableInfo
        {
            FilePath = filePath,
            Schema = schema,
            RecordCount = header.RecordCount,
            Version = header.Version,
            LanguageDriver = header.LanguageDriver,
            Codepage = DbfEncodingHelper.GetCodepage(header.LanguageDriver),
            HasMemoField = header.Fields.Any(f => f.Type is DbfFieldType.Memo or DbfFieldType.Unsupported),
        };
    }

    private static object? DecodeField(byte[] record, DbfFieldDefinition field, Encoding encoding)
    {
        int offset = field.OffsetInRecord;
        int length = field.Length;
        if (offset + length > record.Length)
        {
            return null;
        }

        var span = record.AsSpan(offset, length);

        switch (field.Type)
        {
            case DbfFieldType.Character:
            {
                string text = encoding.GetString(span).TrimEnd('\0', ' ');
                return text;
            }
            case DbfFieldType.Numeric:
            case DbfFieldType.Float:
            {
                string raw = Encoding.ASCII.GetString(span).Trim().Trim('*', '\0');
                if (raw.Length == 0)
                {
                    return null;
                }
                if (decimal.TryParse(raw, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var dec))
                {
                    return dec;
                }
                return raw;
            }
            case DbfFieldType.Date:
            {
                string raw = Encoding.ASCII.GetString(span).Trim('\0', ' ');
                if (raw.Length == 0 || raw == "00000000")
                {
                    return null;
                }
                if (DateTime.TryParseExact(raw, "yyyyMMdd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var date))
                {
                    return date;
                }
                return null;
            }
            case DbfFieldType.DateTime:
            {
                if (length < 8)
                {
                    return null;
                }
                uint julian = BitConverter.ToUInt32(record, offset);
                uint msec = BitConverter.ToUInt32(record, offset + 4);
                if (julian == 0)
                {
                    return null;
                }
                var date = DbfDateTimeCodec.FromJulianDay((int)julian);
                return date.AddMilliseconds(msec);
            }
            case DbfFieldType.Logical:
            {
                char c = (char)record[offset];
                return c switch
                {
                    'T' or 't' or 'Y' or 'y' => true,
                    'F' or 'f' or 'N' or 'n' => false,
                    _ => (bool?)null,
                };
            }
            case DbfFieldType.Integer:
            {
                if (length < 4)
                {
                    return null;
                }
                return BitConverter.ToInt32(record, offset);
            }
            case DbfFieldType.Double:
            {
                if (length < 8)
                {
                    return null;
                }
                return BitConverter.ToDouble(record, offset);
            }
            default:
                return null;
        }
    }

    private sealed class DbfHeaderInfo
    {
        public byte Version { get; set; }
        public int RecordCount { get; set; }
        public int HeaderLength { get; set; }
        public int RecordLength { get; set; }
        public byte LanguageDriver { get; set; }
        public List<DbfFieldDefinition> Fields { get; set; } = new();
    }
}
