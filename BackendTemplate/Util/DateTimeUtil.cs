using NodaTime;
using NodaTime.TimeZones;

namespace BackendTemplate.Util;

public static class DateTimeUtil
{
    // 北京时间04:00为凌晨

    public static DateTimeZone tzShanghai = BclDateTimeZone.FromTimeZoneInfo(TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"));

    public static LocalDateTime Local(this Instant time) => time.InZone(BclDateTimeZone.ForSystemDefault()).LocalDateTime;

    public static LocalDate LocalDay(this Instant time) => time.InZone(BclDateTimeZone.ForSystemDefault()).Date;

    public static Instant Instant(this LocalDateTime time) => time.InZoneLeniently(BclDateTimeZone.ForSystemDefault()).ToInstant();

    public static Instant Now => SystemClock.Instance.GetCurrentInstant();

    public static LocalDate Today() => SystemClock.Instance.GetCurrentInstant().Local().Date;

    public static Instant AddHours(this Instant time, float h) {
        return time.Plus(Duration.FromHours(h));
    }

    public static Instant AddMinutes(this Instant time, float min) {
        return time.Plus(Duration.FromMinutes(min));
    }

    public static Instant AddSeconds(this Instant time, float s) {
        return time.Plus(Duration.FromSeconds(s));
    }

    public static Instant AddDays(this Instant time, int day) {
        return time.Plus(Duration.FromDays(day));
    }

    public static Instant AddWeeks(this Instant time, int num) {
        return time.Plus(Duration.FromDays(num * 7));
    }

    public static Instant AddMonths(this Instant instant, int num) {
        LocalDateTime localDateTime = instant.Local();
        LocalDateTime updatedLocalDateTime = localDateTime.PlusMonths(num);
        Instant updatedInstant = updatedLocalDateTime.Instant();
        return updatedInstant;
    }

    public static Instant AddYears(this Instant time, int year) {
        return time.AddMonths(12 * year);
    }


}