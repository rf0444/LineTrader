using System;

namespace LineTrader.View
{
    public class Line
    {
        public bool Selectable { get; }
        public bool Buy { get; set; }
        public bool Sell { get; set; }
        public decimal Bid { get; }
        public decimal Ask { get; private set; }
        public decimal Spread
        {
            get
            {
                return Ask - Bid;
            }
            set
            {
                Ask = Bid + value;
            }
        }
        public long ChartId { get; }
        public string Name { get; }
        public string Description { get; }
        public string Color { get; }
        public object Identity { get { return ToIdentity(ChartId, Name); } }

        public Line(long chartId, Model.MT4.Line line, decimal? spread, Line old)
        {
            this.ChartId = chartId;
            this.Name = line.name;
            this.Description = line.description;
            this.Color = line.ColorString;
            this.Selectable = true;
            this.Buy = old?.Buy ?? false;
            this.Sell = old?.Sell ?? false;
            this.Bid = line.price;
            this.Ask = line.price + (spread ?? 0);
        }

        public Line(Model.MT4.Price price)
        {
            this.ChartId = 0;
            this.Name = "Current Price";
            this.Description = "";
            this.Color = "white";
            this.Selectable = false;
            this.Buy = false;
            this.Sell = false;
            this.Bid = price.bid;
            this.Ask = price.ask;
        }

        public static object ToIdentity(long chartId, string name)
        {
            return Tuple.Create(chartId, name);
        }
    }
}
