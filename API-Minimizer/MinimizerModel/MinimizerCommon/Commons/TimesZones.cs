namespace MinimizerCommon.Commons;

public class TimesZones
{
    // add a class with array of time zones defined by two properties time and name of the time zone
    // the constuctor will initialize the array with a time zones utc
    public TimeZone[] TimeZones { get; set; }
    public TimesZones()
    {
        TimeZones = new TimeZone[]
        {
            new TimeZone { Name = "UTC", Time = DateTime.UtcNow },
            new TimeZone { Name = "GMT-5", Time = DateTime.UtcNow.AddHours(-5) }
        };
    }
    public class TimeZone
    {
        public string Name { get; set; }
        public DateTime Time { get; set; }
    }
}