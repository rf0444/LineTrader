using LineTrader.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LineTrader.View
{
    public class Lines
    {
        public ObservableSortedList<object, Line> Items { get; }

        private Line current;
        public Line Current
        {
            get
            {
                return this.current;
            }
            set
            {
                var old = this.current;
                this.current = value;
                if (old != null)
                {
                    this.Items.Remove(old);
                }
                if (value != null)
                {
                    this.Items.Add(value);
                }
                if (old?.Spread != value?.Spread)
                {
                    foreach (var item in this.Items)
                    {
                        item.Spread = value?.Spread ?? 0;
                    }
                    this.Items.NotifyReset();
                }
            }
        }

        private Dictionary<object, Line> chartLines;
        public Line this[object id]
        {
            get
            {
                if (id == null)
                {
                    return null;
                }
                if (id.Equals(this.Current?.Identity))
                {
                    return this.Current;
                }
                if (this.chartLines.ContainsKey(id))
                {
                    return this.chartLines[id];
                }
                return null;
            }
        }

        public Lines()
        {
            this.Items = new ObservableSortedList<object, Line>(x => Tuple.Create(-x.Bid, x.Identity));
            this.chartLines = new Dictionary<object, Line>();
        }

        public void UpdateLines(IDictionary<long, Model.MT4.Line[]> charts)
        {
            var newChartLines = new Dictionary<object, Line>();
            if (charts != null)
            {
                foreach (var chart in charts)
                {
                    var chartId = chart.Key;
                    foreach (var line in chart.Value)
                    {
                        var key = Line.ToIdentity(chartId, line.name);
                        var old = this.chartLines.ContainsKey(key) ? this.chartLines[key] : null;
                        newChartLines.Add(key, new Line(chartId, line, this.Current?.Spread, old));
                    }
                }
            }
            this.chartLines = newChartLines;
            if (this.chartLines.Count == 0)
            {
                this.current = null;
            }
            this.Items.Set(this.chartLines.Values.Concat((this.Current == null) ? Enumerable.Empty<Line>() : Enumerable.Repeat(this.Current, 1)));
        }

        public Line StopLossBuy
        {
            get
            {
                if (this.Current == null)
                {
                    return null;
                }
                return this.Items.Tail(this.Current).FirstOrDefault(x => x.Buy && x.Bid < this.Current.Ask);
            }
        }

        public Line TakeProfitBuy
        {
            get
            {
                if (this.Current == null)
                {
                    return null;
                }
                return this.Items.Head(this.Current).FirstOrDefault(x => x.Buy && this.Current.Ask < x.Bid);
            }
        }

        public Line StopLossSell
        {
            get
            {
                if (this.Current == null)
                {
                    return null;
                }
                return this.Items.Head(this.Current).FirstOrDefault(x => x.Sell && this.Current.Bid < x.Ask);
            }
        }

        public Line TakeProfitSell
        {
            get
            {
                if (this.Current == null)
                {
                    return null;
                }
                return this.Items.Tail(this.Current).FirstOrDefault(x => x.Sell && x.Ask < this.Current.Bid);
            }
        }
    }
}
