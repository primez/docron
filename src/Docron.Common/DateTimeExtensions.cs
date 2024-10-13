namespace Docron.Common;

public static class DateTimeExtensions
{
    public static DateTimeOffset? ToLocalTime(this DateTimeOffset? dateTimeOffset)
    {
        return dateTimeOffset?.ToLocalTime() ?? dateTimeOffset;
    }
}