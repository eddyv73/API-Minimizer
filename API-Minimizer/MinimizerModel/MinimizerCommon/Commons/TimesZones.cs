using System;
using System.Collections.Generic;

namespace MinimizerCommon.Commons
{
    /// <summary>
    /// Represents a single timezone entry with a display name and time value.
    /// </summary>
    public class TimeZoneEntry
    {
        public string Time { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Provides a collection of common time zones.
    /// </summary>
    public class TimesZones
    {
        public TimesZones()
        {
            TimeZones = new List<TimeZoneEntry>
            {
                new TimeZoneEntry { Time = DateTime.UtcNow.ToString(), Name = "UTC" },
                new TimeZoneEntry { Time = DateTime.UtcNow.AddHours(-5).ToString(), Name = "GMT-5" }
            };
        }

        public List<TimeZoneEntry> TimeZones { get; set; }
    }
}
