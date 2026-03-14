namespace OscarsBallot.Infrastructure;

public static class BallotEditingPolicy
{
    public static readonly DateTime CeremonyStartMountain = new(2026, 3, 15, 17, 0, 0, DateTimeKind.Unspecified);

    public static bool IsBallotEditingLocked(bool? manualOverride, DateTime utcNow)
    {
        if (manualOverride.HasValue)
        {
            return manualOverride.Value;
        }

        var mountainNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, GetMountainTimeZone());
        return mountainNow >= CeremonyStartMountain;
    }

    private static TimeZoneInfo GetMountainTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Denver");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");
        }
    }
}
