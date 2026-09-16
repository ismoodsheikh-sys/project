namespace VehicleFleetMS.Helpers;

public static class TimeAgoExtensions
{
    public static string ToTimeAgo(this DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalSeconds < 60) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr{((int)span.TotalHours == 1 ? "" : "s")} ago";
        if (span.TotalDays < 2) return "Yesterday";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
        return utc.ToLocalTime().ToString("MMM d, yyyy");
    }
}
