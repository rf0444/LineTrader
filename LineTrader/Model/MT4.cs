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
        public string ColorString
        {
            get
            {
                return String.Format("#{0:x2}{1:x2}{2:x2}", color % 256, color / 256 % 256, color / 256 / 256 % 256);
            }
        }
    }

    public class Price
    {
        public decimal ask { get; set; }
        public decimal bid { get; set; }
        public long time { get; set; }
        public decimal spread
        {
            get
            {
                return ask - bid;
            }
        }
        public DateTime DateTime
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(time).ToLocalTime();
            }
        }
    }

}
