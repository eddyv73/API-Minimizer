namespace MinimizerCommon.Commons;

/// <summary>
/// Provides time zone information with time and name properties
/// </summary>
public class TimesZones
{
    public TimesZones()
    {
        var now = DateTime.Now;
        Zones = new List<TimeZoneData>
        {
            new TimeZoneData { Time = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"), Name = "UTC" },
            new TimeZoneData { Time = now.AddHours(-5).ToString("yyyy-MM-dd HH:mm:ss"), Name = "GMT-5" },
            new TimeZoneData { Time = now.AddHours(-6).ToString("yyyy-MM-dd HH:mm:ss"), Name = "GMT-6" }
        };
    }

    public List<TimeZoneData> Zones { get; set; }
}

/// <summary>
/// Represents time zone data with time and name
/// </summary>
public class TimeZoneData
{
    public string Time { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}