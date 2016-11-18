using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LineTrader
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private Service service;
        private Instrument instrument;
        private Oanda.Position[] positions;

        public MainWindow(Service service)
        {
            this.service = service;
            InitializeComponent();

            service.InstrumentsUpdated.Subscribe(instruments =>
            {
                Dispatcher.Invoke(() =>
                {
                    this.listView_Instruments.DataContext = new ReadOnlyCollection<string>(instruments.ToList());
                });
            });

            service.InstrumentUpdated.Subscribe(instrument =>
            {
                if (this.instrument == null || this.instrument.Name != instrument.Name)
                {
                    return;
                }
                this.instrument = instrument;
                UpdateLines();
            });

            service.OrderPreviewUpdated.Subscribe(t =>
            {
                var buy = t.Item1;
                var sell = t.Item2;
                Dispatcher.Invoke(() =>
                {
                    this.dataGrid_BuySummary.DataContext = OrderSummary.FromOrderPreview(buy);
                    this.dataGrid_SellSummary.DataContext = OrderSummary.FromOrderPreview(sell);
                    this.button_Buy.IsEnabled = buy.Size.OrderSize > 0;
                    this.button_Sell.IsEnabled = sell.Size.OrderSize > 0;
                });
                UpdatePositions();
            });

            service.PositionUpdated.Subscribe(ps =>
            {
                this.positions = ps;
                UpdatePositions();
            });
        }

        private void listView_Instruments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = this.listView_Instruments.SelectedItem as string;
            if (selected == null)
            {
                return;
            }
            var instrument = this.service.GetInstrument(selected);
            if (instrument == null)
            {
                return;
            }
            this.instrument = instrument;
            UpdateLines();
            UpdatePositions();
            this.service.SetInstrument(selected);
        }

        private void Button_Buy_Click(object sender, RoutedEventArgs e)
        {
            this.service.Buy();
            this.tabItem_position.IsSelected = true;
        }

        private void Button_Sell_Click(object sender, RoutedEventArgs e)
        {
            this.service.Sell();
            this.tabItem_position.IsSelected = true;
        }

        private void UpdateLines()
        {
            Dispatcher.Invoke(() =>
            {
                this.dataGrid_Lines.DataContext = (this.instrument.Charts.Count == 0) ? null : new ReadOnlyCollection<Line>(this.instrument.Lines.ToList());
            });
        }

        private void UpdatePositions()
        {
            Dispatcher.Invoke(() =>
            {
                if (this.positions == null)
                {
                    return;
                }
                this.dataGrid_Positions.DataContext = this.positions
                    .SelectMany(x =>
                    {
                        var instrument = this.service.GetInstrument(x.InstrumentName);
                        if (instrument == null)
                        {
                            return Enumerable.Empty<Position>();
                        }
                        var current =
                            (x.side == "buy") ? (instrument?.Current.bid) :
                            (x.side == "sell") ? instrument?.Current.ask : null;
                        var rate = this.service.TransrateToAccountCurrency(1m, instrument.BaseCurrency);
                        var pos = new Position(x, current, rate);
                        return Enumerable.Repeat(pos, 1);
                    })
                    .OrderByDescending(x => x.Instrument == this.instrument?.Name)
                    .ThenBy(x => x.Instrument)
                    .ThenBy(x => x.Side)
                    .ThenByDescending(x => x.Price)
                    .ToArray();
            });
        }

        private void CheckBox_Buy_Changed(object sender, RoutedEventArgs e)
        {
            var c = sender as CheckBox;
            var ctx = c.DataContext as Line;
            ctx.Buy = c.IsChecked ?? false;
            this.service.UpdateOrderPreview();
        }

        private void CheckBox_Sell_Changed(object sender, RoutedEventArgs e)
        {
            var c = sender as CheckBox;
            var ctx = c.DataContext as Line;
            ctx.Sell = c.IsChecked ?? false;
            this.service.UpdateOrderPreview();
        }

        private void dataGrid_Positions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.button_Close.IsEnabled = this.dataGrid_Positions.SelectedItems.Count > 0;
        }

        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            var selecteds = Enumerable.Cast<Position>(this.dataGrid_Positions.SelectedItems)
                .Select(x => x.Id)
                .ToArray();
            this.service.ClosePositions(selecteds);
        }
    }

    public class OrderSummary
    {
        public string Name { get; set; }
        public string Note { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }

        public static OrderSummary[] FromOrderPreview(OrderPreview p)
        {
            var price = p?.Price;
            var size = p?.Size;
            var ret = new List<OrderSummary>
            {
                new OrderSummary
                {
                    Name = "注文",
                    Note = OrderSides.PriceKind(price?.Side),
                    Value = price?.OrderPrice.ToString(),
                    Unit = price?.BaseCurrency,
                },
                new OrderSummary
                {
                    Name = "損切",
                    Note = OrderSides.PriceKind(OrderSides.Inv(price?.Side)),
                    Value = price?.StopLoss.ToString(),
                    Unit = price?.BaseCurrency,
                },
                new OrderSummary
                {
                    Name = "利確",
                    Note = OrderSides.PriceKind(OrderSides.Inv(price?.Side)),
                    Value = price?.TakeProfit.ToString(),
                    Unit = price?.BaseCurrency,
                },
                new OrderSummary
                {
                    Name = "損切幅",
                    Value = price?.StopLossWidth.ToString(),
                    Unit = price?.BaseCurrency,
                },
                new OrderSummary
                {
                    Name = "利確幅",
                    Value = price?.TakeProfitWidth.ToString(),
                    Unit = price?.BaseCurrency,
                },
                new OrderSummary
                {
                    Name = "損益率",
                    Value = string.Format("{0:0.0000}", price?.RiskRewardRatio),
                },
            };
            ret.Add(new OrderSummary
            {
                Name = "許容リスク",
                Note = "余剰証拠金の",
                Value = size?.RiskRatio.ToString(), 
                Unit = "%",
            });
            ret.Add(new OrderSummary
            {
                Value = string.Format("{0:0.0000}", size?.RiskAtAccountCurrency),
                Unit = size?.AccountCurrency,
            });
            if (size != null && price != null && price.BaseCurrency != size.AccountCurrency)
            {
                ret.Add(new OrderSummary
                {
                    Value = string.Format("{0:0.0000}", size?.RiskAtBaseCurrency),
                    Unit = price?.BaseCurrency,
                });
            }
            ret.Add(new OrderSummary
            {
                Name = "注文サイズ",
                Value = size?.OrderSize.ToString(),
                Unit = "通貨",
            });
            return ret.ToArray();
        }
    }

    public class Position
    {
        public long Id { get; set; }
        public decimal Units { get; set; }
        public string Side { get; set; }
        public OrderSide? OrderSide { get { return OrderSides.ToOrderSide(Side); } }
        public string Instrument { get; set; }
        public string DateTime { get; set; }
        public decimal Price { get; set; }
        public decimal? Current { get; set; }
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public decimal? StopLossWidth { get { return (StopLoss == 0) ? null : OrderSide?.Direction() * (Price - StopLoss); } }
        public decimal? TakeProfitWidth { get { return (TakeProfit == 0) ? null : OrderSide?.Direction() * (TakeProfit - Price); } }
        public string RiskRewardRatio { get { return string.Format("{0:0.0000}", TakeProfitWidth / StopLossWidth); } }
        private decimal? rate;
        public string RiskAtAccountCurrency { get { return string.Format("{0:0.0000}", StopLossWidth * Units * rate);} }
        public decimal? StopLossRemain { get { return (StopLoss == 0) ? null : OrderSide?.Direction() * (Current - StopLoss); } }
        public decimal? TakeProfitRemain { get { return (TakeProfit == 0) ? null : OrderSide?.Direction() * (TakeProfit - Current); } }

        public Position(Oanda.Position p, decimal? current, decimal? rate)
        {
            this.Id = p.id;
            this.Units = p.units;
            this.Side = p.side;
            this.Instrument = p.InstrumentName;
            this.DateTime = p.DateTime.ToString();
            this.Price = p.price;
            this.Current = current;
            this.StopLoss = p.stopLoss;
            this.TakeProfit = p.takeProfit;
            this.rate = rate;
        }
    }
}
