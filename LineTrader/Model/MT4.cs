using System;

namespace LineTrader.Model.MT4
{
    public class Command
    {
        public string symbol { get; set; }
        public string Instrument
        {
            get
            {
                return symbol.Insert(3, "/");
            }
        }
        public long chart { get; set; }
        public string operation { get; set; }
        public Price price { get; set; }
        public Line[] lines { get; set; }
    }

    public class Line
    {
        public string name { get; set; }
        public string description { get; set; }
        public decimal price { get; set; }
        public int color { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string ColorString
        {
            get
            {
                return String.Format("#{0:x2}{1:x2}{2:x2}", color % 256, color / 256 % 256, color / 256 / 256 % 256);
            }
        }
        public DateTime? StartDateTime
        {
            get
            {
                return MT4.Environment.ToDateTime(start);
            }
        }
        public DateTime? EndDateTime
        {
            get
            {
                return MT4.Environment.ToDateTime(end);
            }
        }
    }

    public class Price
    {
        public decimal ask { get; set; }
        public decimal bid { get; set; }
        public string time { get; set; }
        public decimal spread
        {
            get
            {
                return ask - bid;
            }
        }
        public DateTime? DateTime
        {
            get
            {
                return MT4.Environment.ToDateTime(time);
            }
        }
    }

    public static class Environment
    {
        public static readonly TimeZoneInfo ChartTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LineTrader.Properties.Settings.Default.MT4TimeZone);

        public static DateTime? ToDateTime(string s)
        {
            if (s == null)
            {
                return null;
            }
            DateTime t;
            if (!DateTime.TryParse(s, out t))
            {
                return null;
            }
            return TimeZoneInfo.ConvertTime(t, ChartTimeZone, TimeZoneInfo.Local);
        }
    }
}
