using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace LineTrader.View
{
    public class OrderPreview
    {
        public string Instrument { get; set; }
        public string BaseCurrency { get; set; }
        public Model.OrderSide Side { get; set; }
        public decimal? OrderPrice { get; set; }
        public decimal? Mt4Price { get; set; }
        public decimal? Mt4StopLoss { get; set; }
        public decimal? Mt4TakeProfit { get; set; }
        public decimal? OrderPriceDifference { get { return OrderPrice - Mt4Price; } }
        public decimal? StopLoss { get { return Mt4StopLoss + OrderPriceDifference; } }
        public decimal? StopLossWidth { get { return Model.OrderSides.Direction(Side) * (OrderPrice - StopLoss); } }
        public decimal? TakeProfit { get { return Mt4TakeProfit + OrderPriceDifference; } }
        public decimal? TakeProfitWidth { get { return Model.OrderSides.Direction(Side) * (TakeProfit - OrderPrice); } }
        public decimal? RiskRewardRatio { get { return (StopLossWidth == 0) ? null : TakeProfitWidth / StopLossWidth; } }

        public decimal OrderRiskRatio { get; set; }
        public decimal? AccountAvail { get; set; }
        public string AccountCurrency { get; set; }
        public decimal? OrderRiskAccountCurrency { get { return AccountAvail * OrderRiskRatio; } }
        public decimal? CurrencyRate { get; set; }
        public decimal? OrderRiskBaseCurrency { get { return OrderRiskAccountCurrency * CurrencyRate; } }
        public int OrderSize { get { return (StopLossWidth == 0) ? 0 : decimal.ToInt32((OrderRiskBaseCurrency / StopLossWidth) ?? 0); } }
    }

    public class OrderPreviewRecord
    {
        public string Name { get; set; }
        public string Note { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
    }
    
    public class OrderPreviewRecords : ObservableCollection<OrderPreviewRecord>
    {
        public OrderPreviewRecord OrderPrice { get; }
        public OrderPreviewRecord StopLoss { get; }
        public OrderPreviewRecord TakeProfit { get; }
        public OrderPreviewRecord StopLossWidth { get; }
        public OrderPreviewRecord TakeProfitWidth { get; }
        public OrderPreviewRecord RiskRewardRatio { get; }
        public OrderPreviewRecord RiskRatio { get; }
        public OrderPreviewRecord RiskAtAccountCurrency { get; }
        public OrderPreviewRecord RiskAtBaseCurrency { get; }
        public OrderPreviewRecord OrderSize { get; }

        public OrderPreviewRecords()
        {
            this.OrderPrice = new OrderPreviewRecord { Name = "注文" };
            this.StopLoss = new OrderPreviewRecord { Name = "損切" };
            this.TakeProfit = new OrderPreviewRecord { Name = "利確" };
            this.StopLossWidth = new OrderPreviewRecord { Name = "損切幅" };
            this.TakeProfitWidth = new OrderPreviewRecord { Name = "利確幅" };
            this.RiskRewardRatio = new OrderPreviewRecord { Name = "損益率" };
            this.RiskRatio = new OrderPreviewRecord {
                Name = "許容リスク",
                Note = "余剰証拠金の",
                Unit = "%",
            };
            this.RiskAtAccountCurrency = new OrderPreviewRecord { };
            this.RiskAtBaseCurrency = new OrderPreviewRecord { };
            this.OrderSize = new OrderPreviewRecord { Name = "注文サイズ", Unit = "通貨" };
            this.Add(this.OrderPrice);
            this.Add(this.StopLoss);
            this.Add(this.TakeProfit);
            this.Add(this.StopLossWidth);
            this.Add(this.TakeProfitWidth);
            this.Add(this.RiskRewardRatio);
            this.Add(this.RiskRatio);
            this.Add(this.RiskAtAccountCurrency);
            this.Add(this.RiskAtBaseCurrency);
            this.Add(this.OrderSize);
        }

        public void Set(OrderPreview order)
        {
            this.OrderPrice.Note = Model.OrderSides.PriceKind(order.Side);
            this.OrderPrice.Value = order.OrderPrice?.ToString();
            this.OrderPrice.Unit = order.BaseCurrency;
            this.StopLoss.Note = Model.OrderSides.PriceKind(Model.OrderSides.Inv(order.Side));
            this.StopLoss.Value = order.StopLoss?.ToString();
            this.StopLoss.Unit = order.BaseCurrency;
            this.TakeProfit.Note = Model.OrderSides.PriceKind(Model.OrderSides.Inv(order.Side));
            this.TakeProfit.Value = order.TakeProfit?.ToString();
            this.TakeProfit.Unit = order.BaseCurrency;
            this.StopLossWidth.Value = order.StopLossWidth?.ToString();
            this.StopLossWidth.Unit = order.BaseCurrency;
            this.TakeProfitWidth.Value = order.TakeProfitWidth?.ToString();
            this.TakeProfitWidth.Unit = order.BaseCurrency;
            this.RiskRewardRatio.Value = order.RiskRewardRatio?.ToString("0.0000");
            this.RiskRatio.Value = (order.OrderRiskRatio * 100).ToString("G29");
            this.RiskAtAccountCurrency.Value = order.OrderRiskAccountCurrency?.ToString("0.0000");
            this.RiskAtAccountCurrency.Unit = order.AccountCurrency;
            this.RiskAtBaseCurrency.Value = (order.AccountCurrency == order.BaseCurrency) ? "" : order.OrderRiskBaseCurrency?.ToString("0.0000");
            this.RiskAtBaseCurrency.Unit = (order.AccountCurrency == order.BaseCurrency) ? "" : order.BaseCurrency;
            this.OrderSize.Value = order.OrderSize.ToString();
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
