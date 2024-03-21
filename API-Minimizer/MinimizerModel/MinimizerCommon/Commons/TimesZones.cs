namespace MinimizerCommon.Commons;

    // add a class with array of time zones defined by two properties time and name of the time zone
    public class TimesZones
    {
            // the constuctor will initialize the array with a time zones utc
            public TimesZones()
            {
                TimeZones = new List<TimeZone>
                {
                    new TimeZone { Time = DateTime.Now.ToUniversalTime().ToString(), Name = "UTC" },
                    new TimeZone { Time = DateTime.Now.AddHours(-5).ToString(), Name = "GMT-5" }
                };
            }
        public string Time { get; set; }
        public string Name { get; set; }
    }