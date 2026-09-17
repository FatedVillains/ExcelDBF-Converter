namespace ExcelDbfConverter.Infrastructure.Dbf;

/// <summary>VFP DateTime 字段与 .NET DateTime 之间的编解码（儒略日 + 毫秒，均小端）。</summary>
public static class DbfDateTimeCodec
{
    // 儒略日：day 0 = 4713 BC-01-01（前推儒略历）。.NET 0001-01-01 对应 JDN 1721426。
    private const int JulianDayOffset = 1_721_426;

    /// <summary>将 .NET DateTime 转换为儒略日编号。</summary>
    public static int ToJulianDay(DateTime value)
    {
        return (value.Date - new DateTime(1, 1, 1)).Days + JulianDayOffset;
    }

    /// <summary>将儒略日编号转换回日期（仅日期部分，时间取午夜）。</summary>
    public static DateTime FromJulianDay(int jdn)
    {
        return new DateTime(1, 1, 1).AddDays(jdn - JulianDayOffset);
    }
}
