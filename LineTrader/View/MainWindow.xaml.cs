using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;

namespace LineTrader.View
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private decimal riskRatio;
        private Model.Service service;
        private Subject<string> instrumentSelected;
        private ReadOnlyReactiveProperty<string> selectedInstrument;

        private Dictionary<string, Lines> lines;

        private Subject<int> orderTabSelected;
        private ReadOnlyReactiveProperty<int> selectedOrderTab;

        private OrderPreview buyOrder;
        private OrderPreview sellOrder;
        private OrderPreviewRecords buyPreview;
        private OrderPreviewRecords sellPreview;
        private Positions positions;

        public MainWindow(Model.Service service, decimal riskRatio)
        {
            InitializeComponent();

            this.riskRatio = riskRatio;
            this.service = service;
            this.instrumentSelected = new Subject<string>();
            this.selectedInstrument = this.instrumentSelected.ToReadOnlyReactiveProperty();

            this.listView_Instruments.DataContext = this.service.Instruments.Keys.OrderBy(x => x);

            var sampling = Observable.Interval(TimeSpan.FromMilliseconds(1000)).Select<long, object>(_ => null);

            this.orderTabSelected = new Subject<int>();
            var orderTabSelected = this.orderTabSelected.Select<int, object>(_ => null);
            this.selectedOrderTab = this.orderTabSelected.ToReadOnlyReactiveProperty();

            this.lines = new Dictionary<string, Lines>();
            foreach (var instrument in this.service.Instruments.Values)
            {
                var lines = new Lines();
                sampling.Subscribe(_ =>
                {
                    var current = instrument.CurrentLine.Value;
                    Dispatcher.Invoke(() =>
                    {
                        var focused = this.dataGrid_Lines.IsKeyboardFocusWithin;
                        var selected = this.dataGrid_Lines.SelectedItem as Line;
                        lines.Current = (current == null) ? null : new Line(current);
                        if (instrument.Name == this.selectedInstrument.Value)
                        {
                            this.dataGrid_Lines.SelectedItem = lines[selected?.Identity];
                        }
                        if (focused)
                        {
                            this.dataGrid_Lines.Focus();
                        }
                    });
                });
                instrument.ChartLines.Subscribe(charts =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var focused = this.dataGrid_Lines.IsKeyboardFocusWithin;
                        var selected = this.dataGrid_Lines.SelectedItem as Line;
                        lines.UpdateLines(charts);
                        if (instrument.Name == this.selectedInstrument.Value)
                        {
                            this.dataGrid_Lines.SelectedItem = lines[selected?.Identity];
                        }
                        if (focused)
                        {
                            this.dataGrid_Lines.Focus();
                        }
                    });
                });
                this.lines.Add(instrument.Name, lines);
            }
            this.dataGrid_Lines.DataContext = this.instrumentSelected
                .Select(name => (name == null) ? null : this.lines[name].Items)
                .Do(_ =>
                {
                    var current = this.lines[this.selectedInstrument.Value].Current;
                    if (current == null)
                    {
                        return;
                    }
                    this.dataGrid_Lines.SelectedItem = current;
                    this.dataGrid_Lines.Focus();
                })
                .ToReadOnlyReactiveProperty()
            ;

            this.buyPreview = new OrderPreviewRecords();
            this.sellPreview = new OrderPreviewRecords();
            this.dataGrid_BuyPreview.DataContext = this.buyPreview;
            this.dataGrid_SellPreview.DataContext = this.sellPreview;
            Observable
                .Merge(
                    this.lines
                        .Select(x => x.Value.Items.CollectionChangedAsObservable().Select(_ => x.Key))
                        .Merge()
                        .Where(x => x == this.selectedInstrument.Value),
                    this.instrumentSelected,
                    orderTabSelected,
                    this.service.Account
                )
                .Where(_ => this.selectedOrderTab.Value == 0)
                .Subscribe(_ => UpdateOrderPerview())
            ;
            this.positions = new Positions();
            this.dataGrid_Positions.DataContext = this.positions.Items;
            Observable
                .Merge(
                    sampling,
                    this.instrumentSelected,
                    orderTabSelected,
                    this.service.Positions
                )
                .Where(_ => this.selectedOrderTab.Value == 1)
                .Subscribe(_ => UpdatePositions())
            ;
        }

        private void listView_Instruments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = this.listView_Instruments.SelectedItem as string;
            if (selected == null)
            {
                return;
            }
            this.instrumentSelected.OnNext(selected);
        }

        private void CheckBox_Buy_Changed(object sender, RoutedEventArgs e)
        {
            var c = sender as CheckBox;
            var ctx = c.DataContext as Line;
            ctx.Buy = c.IsChecked ?? false;
            this.lines[this.selectedInstrument.Value].Items.NotifyReplaced(ctx);
        }

        private void CheckBox_Sell_Changed(object sender, RoutedEventArgs e)
        {
            var c = sender as CheckBox;
            var ctx = c.DataContext as Line;
            ctx.Sell = c.IsChecked ?? false;
            this.lines[this.selectedInstrument.Value].Items.NotifyReplaced(ctx);
        }

        private void Button_Buy_Click(object sender, RoutedEventArgs e)
        {
            if (this.buyOrder == null)
            {
                return;
            }
            this.service.Order(new Model.Oanda.Order
            {
                Instrument = this.buyOrder.Instrument,
                Units = this.buyOrder.OrderSize,
                Side = Model.OrderSide.Buy,
                StopLoss = this.buyOrder.StopLoss,
                TakeProfit = this.buyOrder.TakeProfit,
            });
            this.tabItem_position.IsSelected = true;
        }

        private void Button_Sell_Click(object sender, RoutedEventArgs e)
        {
            if (this.sellOrder == null)
            {
                return;
            }
            this.service.Order(new Model.Oanda.Order
            {
                Instrument = this.sellOrder.Instrument,
                Units = this.sellOrder.OrderSize,
                Side = Model.OrderSide.Sell,
                StopLoss = this.sellOrder.StopLoss,
                TakeProfit = this.sellOrder.TakeProfit,
            });
            this.tabItem_position.IsSelected = true;
        }

        private void tabControl_order_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.orderTabSelected.OnNext(this.tabControl_order.SelectedIndex);
        }

        private void dataGrid_Positions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.button_Close.IsEnabled = this.dataGrid_Positions.SelectedItems.Count > 0;
            e.Handled = true;
        }

        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            var selecteds = Enumerable.Cast<Position>(this.dataGrid_Positions.SelectedItems).Select(x => x.Id);
            this.service.ClosePositions(selecteds);
        }

        private void UpdateOrderPerview()
        {
            var name = this.selectedInstrument.Value;
            var lines = (name == null) ? null : this.lines[name];
            var instrument = (name == null) ? null : this.service.Instruments[name];
            var price = instrument?.Price.Value;
            var account = this.service.Account.Value;
            this.buyOrder = new OrderPreview
            {
                Instrument = name,
                BaseCurrency = instrument?.BaseCurrency,
                Side = Model.OrderSide.Buy,
                OrderPrice = price?.ask,
                Mt4Price = lines?.Current?.Ask,
                Mt4StopLoss = lines?.StopLossBuy?.Bid,
                Mt4TakeProfit = lines?.TakeProfitBuy?.Bid,
                OrderRiskRatio = this.riskRatio / 100,
                AccountCurrency = account?.accountCurrency,
                AccountAvail = account?.marginAvail,
                CurrencyRate = this.service.Transrate(1, account?.accountCurrency, instrument?.BaseCurrency),
            };
            this.sellOrder = new OrderPreview
            {
                Instrument = name,
                BaseCurrency = instrument?.BaseCurrency,
                Side = Model.OrderSide.Sell,
                OrderPrice = price?.bid,
                Mt4Price = lines?.Current?.Bid,
                Mt4StopLoss = lines?.StopLossSell?.Ask,
                Mt4TakeProfit = lines?.TakeProfitSell?.Ask,
                OrderRiskRatio = this.riskRatio / 100,
                AccountCurrency = account?.accountCurrency,
                AccountAvail = account?.marginAvail,
                CurrencyRate = this.service.Transrate(1, account?.accountCurrency, instrument?.BaseCurrency),
            };
            Dispatcher.Invoke(() =>
            {
                this.buyPreview.Set(this.buyOrder);
                this.sellPreview.Set(this.sellOrder);
                this.button_Buy.IsEnabled = this.buyOrder.OrderSize > 0;
                this.button_Sell.IsEnabled = this.sellOrder.OrderSize > 0;
            });
        }

        public void UpdatePositions()
        {
            var positions = this.service.Positions.Value;
            var account = this.service.Account.Value;
            var view = (positions == null) ? new Position[] { } : positions.Select(model =>
            {
                var instrument = this.service.Instruments[model.InstrumentName];
                var price = instrument.Price.Value;
                var rate = this.service.Transrate(1, instrument.BaseCurrency, account?.accountCurrency);
                return new Position(model, price, rate);
            });
            Dispatcher.Invoke(() =>
            {
                var focused = this.dataGrid_Positions.IsKeyboardFocusWithin;
                var selecteds = Enumerable.Cast<Position>(this.dataGrid_Positions.SelectedItems).ToArray();
                this.positions.Set(view);
                foreach (var selected in selecteds)
                {
                    this.dataGrid_Positions.SelectedItems.Add(this.positions[selected.Id]);
                }
                if (focused)
                {
                    this.dataGrid_Positions.Focus();
                }
            });
        }
    }
}
