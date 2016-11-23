using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace LineTrader.Model
{
    public class Instrument
    {
        public string Name { get; }
        public string BaseCurrency
        {
            get
            {
                return Name.Split('/')[1];
            }
        }
        public ReadOnlyReactiveProperty<Oanda.Price> Price { get; }
        public ReadOnlyReactiveProperty<MT4.Price> CurrentLine { get; }
        public ReadOnlyReactiveProperty<ReadOnlyDictionary<long, MT4.Line[]>> ChartLines { get; }

        public Instrument(string name, IObservable<Oanda.Price> prices, IObservable<MT4.Command> commands)
        {
            this.Name = name;
            this.Price = prices.ToReadOnlyReactiveProperty();
            this.CurrentLine = commands
                .Where(c => c.price != null || c.operation == "close")
                .Select(c => (c.operation == "close") ? null : c.price)
                .ToReadOnlyReactiveProperty()
            ;
            this.ChartLines = commands
                .Where(c => c.lines != null || c.operation == "close")
                .Scan(new Dictionary<long, MT4.Line[]>(), (a, c) =>
                {
                    var ret = new Dictionary<long, MT4.Line[]>(a);
                    if (c.operation == "close")
                    {
                        ret.Remove(c.chart);
                    }
                    else
                    {
                        ret[c.chart] = c.lines;
                    }
                    return ret;
                })
                .Select(d => new ReadOnlyDictionary<long, MT4.Line[]>(d))
                .ToReadOnlyReactiveProperty()
            ;
        }
    }
}
