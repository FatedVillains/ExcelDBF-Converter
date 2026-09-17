using System.Text;
using ExcelDbfConverter.Core.Constants;
using ExcelDbfConverter.Core.Enums;
using ExcelDbfConverter.Core.Interfaces;
using ExcelDbfConverter.Core.Models;

namespace ExcelDbfConverter.Infrastructure.Dbf;

/// <summary>Visual FoxPro DBF 写入器。</summary>
public sealed class DbfWriter : IDbfWriter, IDisposable
{
    private FileStream? _stream;
    private Encoding _encoding = Encoding.UTF8;
    private byte _languageDriver;
    private int _recordLength;
    private int _recordCount;
    private List<DbfFieldDefinition> _fields = new();

    public Task CreateAsync(
        string filePath,
        DbfTableSchema schema,
        DbfEncodingKind encodingKind,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _fields = new List<DbfFieldDefinition>(schema.Fields);
        _encoding = DbfEncodingHelper.GetEncoding(encodingKind);
        _languageDriver = DbfEncodingHelper.GetLanguageDriverByteForWrite(encodingKind);

        // 计算字段偏移与记录长度。
        int offset = 1;
        foreach (var field in _fields)
        {
            field.OffsetInRecord = offset;
            offset += field.Length;
        }
        _recordLength = offset;
        _recordCount = 0;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);
        WriteHeader();
        return Task.CompletedTask;
    }

    public Task WriteRecordAsync(DbfRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_stream is null)
        {
            throw new InvalidOperationException("写入器尚未创建。");
        }

        var buffer = new byte[_recordLength];
        buffer[0] = record.IsDeleted ? DbfConstants.RecordDeleted : DbfConstants.RecordActive;

        for (int i = 0; i < _fields.Count; i++)
        {
            object? value = i < record.Values.Count ? record.Values[i] : null;
            EncodeField(buffer, _fields[i], value);
        }

        _stream.Write(buffer, 0, buffer.Length);
        _recordCount++;
        return Task.CompletedTask;
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (_stream is null)
        {
            return;
        }

        // 写入文件结束标记 0x1A。
        _stream.WriteByte(DbfConstants.EndOfFileMarker);

        // 回填记录数（表头偏移 4，4 字节小端）。
        long position = _stream.Position;
        _stream.Seek(4, SeekOrigin.Begin);
        var countBytes = BitConverter.GetBytes(_recordCount);
        await _stream.WriteAsync(countBytes, cancellationToken).ConfigureAwait(false);
        _stream.Seek(position, SeekOrigin.Begin);

        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        await _stream.DisposeAsync().ConfigureAwait(false);
        _stream = null;
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }

    private void WriteHeader()
    {
        if (_stream is null)
        {
            return;
        }

        int fieldCount = _fields.Count;
        int headerLength = DbfConstants.HeaderPrefixSize
            + fieldCount * DbfConstants.FieldDescriptorSize
            + DbfConstants.HeaderTerminatorSize
            + DbfConstants.HeaderBacklinkSize;

        var header = new byte[headerLength];

        header[0] = DbfConstants.VfpVersion;

        var now = DateTime.Now;
        header[1] = (byte)(now.Year % 100);
        header[2] = (byte)now.Month;
        header[3] = (byte)now.Day;

        // 记录数初始为 0。
        BitConverter.GetBytes((ushort)headerLength).CopyTo(header, 8);
        BitConverter.GetBytes((ushort)_recordLength).CopyTo(header, 10);

        header[28] = 0; // MDX 标志：无 .cdx。
        header[29] = _languageDriver;

        int position = DbfConstants.HeaderPrefixSize;
        foreach (var field in _fields)
        {
            WriteFieldDescriptor(header, position, field);
            position += DbfConstants.FieldDescriptorSize;
        }

        header[position] = 0x0D; // 字段描述符终止符。
        // 其余 backlink 区保持 0。

        _stream.Write(header, 0, header.Length);
    }

    private static void WriteFieldDescriptor(byte[] header, int position, DbfFieldDefinition field)
    {
        var nameBytes = Encoding.ASCII.GetBytes(field.Name);
        int nameLength = Math.Min(nameBytes.Length, 10);
        Array.Copy(nameBytes, 0, header, position, nameLength);
        header[position + 10] = 0; // 名称以 null 结束。

        header[position + 11] = (byte)field.Type.ToDbfCode();
        BitConverter.GetBytes(field.OffsetInRecord).CopyTo(header, position + 12);
        header[position + 16] = (byte)field.Length;
        header[position + 17] = (byte)field.DecimalCount;
    }

    private void EncodeField(byte[] buffer, DbfFieldDefinition field, object? value)
    {
        int offset = field.OffsetInRecord;

        switch (field.Type)
        {
            case DbfFieldType.Character:
            {
                var bytes = _encoding.GetBytes(value?.ToString() ?? string.Empty);
                Array.Fill(buffer, (byte)' ', offset, field.Length);
                int copy = Math.Min(bytes.Length, field.Length);
                Array.Copy(bytes, 0, buffer, offset, copy);
                break;
            }
            case DbfFieldType.Numeric:
            case DbfFieldType.Float:
            {
                string text = FormatNumeric(value, field);
                var bytes = Encoding.ASCII.GetBytes(text);
                Array.Fill(buffer, (byte)' ', offset, field.Length);
                int copy = Math.Min(bytes.Length, field.Length);
                // 数值右对齐。
                Array.Copy(bytes, 0, buffer, offset + field.Length - copy, copy);
                break;
            }
            case DbfFieldType.Date:
            {
                string text = value is DateTime dt ? dt.ToString("yyyyMMdd") : new string(' ', 8);
                var bytes = Encoding.ASCII.GetBytes(text);
                Array.Copy(bytes, 0, buffer, offset, Math.Min(bytes.Length, field.Length));
                break;
            }
            case DbfFieldType.DateTime:
            {
                if (value is DateTime dt)
                {
                    uint julian = (uint)DbfDateTimeCodec.ToJulianDay(dt);
                    uint msec = (uint)(dt.TimeOfDay.TotalMilliseconds);
                    BitConverter.GetBytes(julian).CopyTo(buffer, offset);
                    BitConverter.GetBytes(msec).CopyTo(buffer, offset + 4);
                }
                else
                {
                    Array.Clear(buffer, offset, 8);
                }
                break;
            }
            case DbfFieldType.Logical:
            {
                buffer[offset] = value switch
                {
                    true => (byte)'T',
                    false => (byte)'F',
                    _ => (byte)' ',
                };
                break;
            }
            case DbfFieldType.Integer:
            {
                int v = value switch
                {
                    int i => i,
                    decimal d => (int)d,
                    double dbl => (int)dbl,
                    string s when int.TryParse(s, out var parsed) => parsed,
                    _ => 0,
                };
                BitConverter.GetBytes(v).CopyTo(buffer, offset);
                break;
            }
            case DbfFieldType.Double:
            {
                double v = value switch
                {
                    double dbl => dbl,
                    decimal d => (double)d,
                    int i => i,
                    float f => f,
                    string s when double.TryParse(s, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed) => parsed,
                    _ => 0,
                };
                BitConverter.GetBytes(v).CopyTo(buffer, offset);
                break;
            }
            default:
                break;
        }
    }

    private static string FormatNumeric(object? value, DbfFieldDefinition field)
    {
        if (value is null)
        {
            return string.Empty;
        }

        string text;
        if (value is decimal dec)
        {
            text = dec.ToString($"F{field.DecimalCount}", System.Globalization.CultureInfo.InvariantCulture);
        }
        else if (double.TryParse(
                     Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
                     System.Globalization.NumberStyles.Float,
                     System.Globalization.CultureInfo.InvariantCulture, out var dbl))
        {
            text = dbl.ToString($"F{field.DecimalCount}", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            return string.Empty;
        }

        if (text.Length > field.Length)
        {
            // 超出长度：VFP 约定用星号填充。
            return new string('*', field.Length);
        }

        return text;
    }
}
