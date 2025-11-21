namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// DateTimeOffset 到 Unix 时间戳（毫秒）的值转换器
/// </summary>
/// <remarks>
/// SQLite 不支持 DateTimeOffset 类型，将其转换为 long 类型的 Unix 时间戳（毫秒）存储
/// </remarks>
public class DateTimeOffsetToUnixMillisecondsConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetToUnixMillisecondsConverter()
        : base(
            v => v.ToUnixTimeMilliseconds(),
            v => DateTimeOffset.FromUnixTimeMilliseconds(v))
    {
    }
}

/// <summary>
/// 可空 DateTimeOffset 到可空 Unix 时间戳（毫秒）的值转换器
/// </summary>
/// <remarks>
/// SQLite 不支持 DateTimeOffset 类型，将其转换为 long 类型的 Unix 时间戳（毫秒）存储
/// </remarks>
public class NullableDateTimeOffsetToUnixMillisecondsConverter : ValueConverter<DateTimeOffset?, long?>
{
    public NullableDateTimeOffsetToUnixMillisecondsConverter()
        : base(
            v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : null,
            v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null)
    {
    }
}
