using System;

namespace Nellebot.Utils;

public static class DateTimeExtensions
{
    public static string ToIsoDateTimeString(this DateTimeOffset dateTimeOffset)
    {
        return $"{dateTimeOffset:yyyy-MM-dd HH:mm}";
    }
}
