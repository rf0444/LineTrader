using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace LineTrader.Model
{
    public class Service : IDisposable
    {
        private Oanda.RestClient restClient;

        public ReadOnlyDictionary<string, Instrument> Instruments { get; }
        public ReadOnlyReactiveProperty<Oanda.Account> Account { get; }
        public ReadOnlyReactiveProperty<Oanda.Position[]> Positions { get; }

        private Subject<MT4.Command> mt4Commands;

        public Service(Oanda.RestClient restClient, string[] instruments)
        {
            this.restClient = restClient;
            this.mt4Commands = new Subject<MT4.Command>();
            var prices = this.restClient.GetPriceStream(instruments);
            this.Instruments = new ReadOnlyDictionary<string, Instrument>(
                instruments
                    .Select(name => {
                        var ps = prices.Where(p => p.InstrumentName == name);
                        var cs = this.mt4Commands.Where(c => c.Instrument == name);
                        return new Instrument(name, ps, cs);
                    })
                    .ToDictionary(instrument => instrument.Name)
            );
            var events = this.restClient.GetEventStream().Merge(Observable.Return<object>(null));
            this.Account = events
                .SelectMany(_ => this.restClient.GetAccount())
                .ToReadOnlyReactiveProperty()
            ;
            this.Positions = events
                .SelectMany(_ => this.restClient.GetPositions())
                .ToReadOnlyReactiveProperty()
            ;
        }

        public HttpStatusCode Apply(MT4.Command c)
        {
            if (!this.Instruments.ContainsKey(c.Instrument))
            {
                return HttpStatusCode.NotFound;
            }
            var instrument = this.Instruments[c.Instrument];
            var chart = instrument.ChartLines.Value;
            if ((chart == null || chart.Count == 0) && c.operation != "init")
            {
                return HttpStatusCode.PreconditionFailed;
            }
            Task.Run(() =>
            {
                this.mt4Commands.OnNext(c);
            });
            return HttpStatusCode.OK;
        }

        public void Dispose()
        {
            this.restClient.Dispose();
        }
        
        public void Order(Oanda.Order order)
        {
            this.restClient.SendOrder(order);
        }

        public void ClosePositions(IEnumerable<long> ids)
        {
            foreach (var id in ids)
            {
                this.restClient.ClosePosition(id);
            }
        }

        public decimal? Transrate(decimal? price, string from, string to)
        {
            if (price == null || from == null || to == null)
            {
                return null;
            }
            if (from == to)
            {
                return price;
            }
            if (this.Instruments.ContainsKey(from + "/" + to))
            {
                return price * this.Instruments[from + "/" + to].Price.Value?.ask;
            }
            if (this.Instruments.ContainsKey(to + "/" + from))
            {
                return price / this.Instruments[to + "/" + from].Price.Value?.bid;
            }
            return null;
        }
    }
}
