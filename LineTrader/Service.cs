using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace LineTrader
{
    public class Service : IDisposable
    {
        private Oanda.RestClient restClient;
        private decimal riskRatio;

        private Dictionary<string, Instrument> instruments;
        private Subject<string[]> instrumentsUpdated;
        public IObservable<string[]> InstrumentsUpdated { get { return instrumentsUpdated; } }

        private string instrument;
        private Subject<Instrument> instrumentUpdated;
        public IObservable<Instrument> InstrumentUpdated { get { return instrumentUpdated; } }

        private OrderPreview buyOrderPreview;
        private OrderPreview sellOrderPreview;
        private Subject<Tuple<OrderPreview, OrderPreview>> orderPreviewUpdated;
        public IObservable<Tuple<OrderPreview, OrderPreview>> OrderPreviewUpdated { get { return orderPreviewUpdated; } }

        private Oanda.Account account;
        private Subject<Oanda.Account> accountUpdated;
        public IObservable<Oanda.Account> AccountUpdated { get { return accountUpdated; } }

        private Oanda.Position[] positions;
        private Subject<Oanda.Position[]> positionUpdated;
        public IObservable<Oanda.Position[]> PositionUpdated { get { return positionUpdated; } }


        public Service(Oanda.RestClient restClient, decimal riskRatio)
        {
            this.riskRatio = riskRatio;
            this.restClient = restClient;
            this.instruments = new Dictionary<string, Instrument>();

            this.instrumentsUpdated = new Subject<string[]>();
            this.instrumentUpdated = new Subject<Instrument>();
            this.orderPreviewUpdated = new Subject<Tuple<OrderPreview, OrderPreview>>();
            this.accountUpdated = new Subject<Oanda.Account>();
            this.positionUpdated = new Subject<Oanda.Position[]>();
        }

        public Instrument GetInstrument(string instrument)
        {
            return (this.instruments.ContainsKey(instrument)) ? this.instruments[instrument] : null;
        }

        public void Start(string[] instruments)
        {
            this.restClient.GetPriceStream(instruments).Subscribe(price =>
            {
                var name = price.InstrumentName;
                var instrument = GetInstrument(name);
                if (instrument == null)
                {
                    this.instruments[name] = new Instrument(price);
                }
                else
                {
                    this.instruments[name].Current = price;
                }
                UpdateOrderPreview();
            });
            this.instrumentsUpdated.OnNext(instruments);
            this.restClient.GetEventStream().Subscribe(_ =>
            {
                UpdateAccount();
            });
            UpdateAccount();
        }

        private void UpdateAccount()
        {
            this.restClient.GetAccount().ContinueWith(account =>
            {
                this.account = account.Result;
                this.accountUpdated.OnNext(this.account);
                UpdateOrderPreview();
            });
            this.restClient.GetPositions().ContinueWith(positions =>
            {
                this.positions = positions.Result;
                this.positionUpdated.OnNext(this.positions);
            });
        }

        public bool Apply(MT4.Command c)
        {
            var instrument = GetInstrument(c.Instrument);
            if ((instrument == null || instrument.Charts.Count == 0) && c.operation != "init")
            {
                return false;
            }
            if (instrument == null &&  c.operation == "init")
            {
                var newInstrument = new Instrument(c);
                this.instruments[c.Instrument] = newInstrument;
                this.instrumentUpdated.OnNext(newInstrument);
                UpdateOrderPreview();
                return true;
            }
            instrument.Apply(c);
            this.instrumentUpdated.OnNext(instrument);
            UpdateOrderPreview();
            return true;
        }

        public void SetInstrument(string instrument)
        {
            this.instrument = instrument;
            UpdateOrderPreview();
        }

        public void Dispose()
        {
            this.restClient.Dispose();
        }

        public void UpdateOrderPreview()
        {
            var riskAtAccountCurrency = this.account?.marginAvail * this.riskRatio / 100 ?? 0m;
            if (this.instrument == null || !this.instruments.ContainsKey(this.instrument))
            {
                var p = new OrderPreview {
                    Size = new OrderSizePreview
                    {
                        AccountCurrency = this.account?.accountCurrency,
                        RiskRatio = this.riskRatio,
                        RiskAtAccountCurrency = riskAtAccountCurrency,
                    },
                };
                this.orderPreviewUpdated.OnNext(Tuple.Create(p, p));
                return;
            }
            var instrument = this.instruments[this.instrument];
            var baseCurrency = instrument.BaseCurrency;
            Line buySL = null;
            Line buyTP = null;
            Line sellSL = null;
            Line sellTP = null;
            if (instrument.CurrentLine != null)
            {
                foreach (var line in instrument.Lines)
                {
                    if (line.Buy)
                    {
                        if ((buySL == null || buySL.Bid < line.Bid) && line.Bid < instrument.CurrentLine.Ask)
                        {
                            buySL = line;
                        }
                        if (instrument.CurrentLine.Ask < line.Bid && (buyTP == null || line.Bid < buyTP.Bid))
                        {
                            buyTP = line;
                        }
                    }
                    if (line.Sell)
                    {
                        if (instrument.CurrentLine.Bid < line.Ask && (sellSL == null || line.Ask < sellSL.Ask))
                        {
                            sellSL = line;
                        }
                        if ((sellTP == null || sellTP.Ask < line.Ask) && line.Ask < instrument.CurrentLine.Bid)
                        {
                            sellTP = line;
                        }
                    }
                }
            }
            var buyPrice = new OrderPricePreview
            {
                BaseCurrency = baseCurrency,
                Side = OrderSide.Buy,
                OrderPrice = instrument.Current?.ask,
                Mt4Price = instrument.CurrentLine?.Ask,
                Mt4StopLoss = buySL?.Bid,
                Mt4TakeProfit = buyTP?.Bid,
            };
            var sellPrice = new OrderPricePreview
            {
                BaseCurrency = baseCurrency,
                Side = OrderSide.Sell,
                OrderPrice = instrument.Current?.bid,
                Mt4Price = instrument.CurrentLine?.Bid,
                Mt4StopLoss = sellSL?.Ask,
                Mt4TakeProfit = sellTP?.Ask,
            };
            var riskAtBaseCurrency = TransrateFromAccountCurrency(riskAtAccountCurrency, baseCurrency) ?? 0m;
            var buySize = new OrderSizePreview
            {
                AccountCurrency = this.account?.accountCurrency,
                RiskRatio = this.riskRatio,
                RiskAtAccountCurrency = riskAtAccountCurrency,
                RiskAtBaseCurrency = riskAtBaseCurrency,
                OrderSize = decimal.ToInt32((riskAtBaseCurrency / buyPrice.StopLossWidth) ?? 0),
            };
            var sellSize = new OrderSizePreview
            {
                AccountCurrency = this.account?.accountCurrency,
                RiskRatio = this.riskRatio,
                RiskAtAccountCurrency = riskAtAccountCurrency,
                RiskAtBaseCurrency = riskAtBaseCurrency,
                OrderSize = decimal.ToInt32((riskAtBaseCurrency / sellPrice.StopLossWidth) ?? 0),
            };
            this.buyOrderPreview = new OrderPreview { Price = buyPrice, Size = buySize };
            this.sellOrderPreview = new OrderPreview { Price = sellPrice, Size = sellSize };
            this.orderPreviewUpdated.OnNext(Tuple.Create(this.buyOrderPreview, this.sellOrderPreview));
        }

        public decimal? TransrateToAccountCurrency(decimal? price, string from)
        {
            return Transrate(price, from, this.account?.accountCurrency);
        }

        public decimal? TransrateFromAccountCurrency(decimal? price, string to)
        {
            return Transrate(price, this.account?.accountCurrency, to);
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
            if (this.instruments.ContainsKey(from + "/" + to))
            {
                return price * this.instruments[from + "/" + to].Current?.ask;
            }
            if (this.instruments.ContainsKey(to + "/" + from))
            {
                return price /  this.instruments[to + "/" + from].Current?.bid;
            }
            return null;
        }

        public void Buy()
        {
            if (this.buyOrderPreview == null)
            {
                return;
            }
            this.restClient.SendOrder(new Oanda.Order
            {
                Instrument = this.instrument,
                Units = this.buyOrderPreview.Size.OrderSize,
                Side = OrderSide.Buy,
                StopLoss = this.buyOrderPreview.Price.StopLoss,
                TakeProfit = this.buyOrderPreview.Price.TakeProfit,
            }).ContinueWith(s =>
            {
                Console.WriteLine(s.Result);
            });
        }

        public void Sell()
        {
            if (this.sellOrderPreview == null)
            {
                return;
            }
            this.restClient.SendOrder(new Oanda.Order
            {
                Instrument = this.instrument,
                Units = this.sellOrderPreview.Size.OrderSize,
                Side = OrderSide.Sell,
                StopLoss = this.sellOrderPreview.Price.StopLoss,
                TakeProfit = this.sellOrderPreview.Price.TakeProfit,
            });
        }

        public void ClosePositions(long[] ids)
        {
            foreach (var id in ids)
            {
                this.restClient.ClosePosition(id);
            }
        }
    }

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
        public Oanda.Price Current { get; set; }
        public Line CurrentLine { get; set; }
        public Dictionary<long, Line[]> Charts { get; set; }
        public IEnumerable<Line> Lines
        {
            get
            {
                return Charts.Values.SelectMany(x => x)
                    .Concat(new[] { CurrentLine })
                    .OrderByDescending(x => x.Bid)
                    .ThenBy(x => x.Name)
                ;
            }
        }

        public Instrument(Oanda.Price p)
        {
            this.Name = p.InstrumentName;
            this.Charts = new Dictionary<long, Line[]>();
        }

        public Instrument(MT4.Command c)
        {
            this.Name = c.Instrument;
            this.Charts = new Dictionary<long, Line[]>();
            this.Apply(c);
        }

        public void Apply(MT4.Command c)
        {
            if (this.Name != c.Instrument)
            {
                return;
            }
            if (c.operation == "close")
            {
                this.Charts.Remove(c.chart);
                return;
            }
            var currentSpread = (this.CurrentLine == null) ? 0 : this.CurrentLine.Ask - this.CurrentLine.Bid;
            if (c.price != null)
            {
                this.CurrentLine = new Line
                {
                    Selectable = false,
                    Bid = c.price.bid,
                    Ask = c.price.ask,
                    Color = "white",
                    Name = "Current Price",
                };
            }
            var newSpread = (this.CurrentLine == null) ? 0 : this.CurrentLine.Ask - this.CurrentLine.Bid;
            if (c.lines != null)
            {
                var lineMap = (this.Charts.ContainsKey(c.chart) ? this.Charts[c.chart].ToDictionary(x => x.Name) : new Dictionary<string, Line>());
                var lines = c.lines.Select(x => new Line
                {
                    Selectable = true,
                    Buy = (lineMap.ContainsKey(x.name)) ? lineMap[x.name].Buy : false,
                    Sell = (lineMap.ContainsKey(x.name)) ? lineMap[x.name].Sell : false,
                    Bid = x.price,
                    Ask = x.price + newSpread,
                    Color = x.ColorString,
                    Name = x.name,
                    Description = x.description,
                });
                this.Charts[c.chart] = lines.ToArray();
            }
            if (currentSpread != newSpread)
            {
                foreach (var chart in this.Charts)
                {
                    if (chart.Key == c.chart)
                    {
                        continue;
                    }
                    foreach (var line in chart.Value)
                    {
                        line.Ask = line.Bid + newSpread;
                    }
                }
            }
        }
    }

    public class Line
    {
        public bool Selectable { get; set; }
        public bool Buy { get; set; }
        public bool Sell { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public string Color { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class OrderPreview
    {
        public OrderPricePreview Price { get; set; }
        public OrderSizePreview Size { get; set; }
    }
    public class OrderPricePreview
    {
        public string BaseCurrency { get; set; }
        public OrderSide Side { get; set; }
        public decimal? OrderPrice { get; set; }
        public decimal? Mt4Price { get; set; }
        public decimal? Mt4StopLoss { get; set; }
        public decimal? Mt4TakeProfit { get; set; }
        public decimal? StopLoss { get { return Mt4StopLoss - Mt4Price + OrderPrice; } }
        public decimal? StopLossWidth { get { return Side.Direction() * (OrderPrice - StopLoss); } }
        public decimal? TakeProfit { get { return Mt4TakeProfit - Mt4Price + OrderPrice; } }
        public decimal? TakeProfitWidth { get { return Side.Direction() * (TakeProfit - OrderPrice); } }
        public decimal? RiskRewardRatio { get { return TakeProfitWidth / StopLossWidth; } }
    }
    public class OrderSizePreview
    {
        public string AccountCurrency { get; set; }
        public decimal RiskRatio { get; set; }
        public decimal RiskAtAccountCurrency { get; set; }
        public decimal RiskAtBaseCurrency { get; set; }
        public int OrderSize { get; set; }
    }
}
