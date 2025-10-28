using System;
using System.Collections.Generic;

namespace API_Minimizer_back.Models
{
    public class EspnScoreboardResponse
    {
        public List<Event> Events { get; set; }
        public League League { get; set; }
        public int Year { get; set; }
        public Season Season { get; set; }
    }

    public class Event
    {
        public string Id { get; set; }
        public string Uid { get; set; }
        public string Date { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public List<Competition> Competitions { get; set; }
        public Status Status { get; set; }
    }

    public class Competition
    {
        public string Id { get; set; }
        public string Uid { get; set; }
        public string Date { get; set; }
        public List<Competitor> Competitors { get; set; }
        public Status Status { get; set; }
    }

    public class Competitor
    {
        public string Id { get; set; }
        public string Uid { get; set; }
        public string Type { get; set; }
        public int Order { get; set; }
        public string HomeAway { get; set; }
        public Team Team { get; set; }
        public string Score { get; set; }
    }

    public class Team
    {
        public string Id { get; set; }
        public string Uid { get; set; }
        public string DisplayName { get; set; }
        public string ShortDisplayName { get; set; }
        public string Abbreviation { get; set; }
        public string Logo { get; set; }
    }

    public class Status
    {
        public string Type { get; set; }
    }

    public class League
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
    }

    public class Season
    {
        public int Year { get; set; }
        public string Type { get; set; }
    }
}
