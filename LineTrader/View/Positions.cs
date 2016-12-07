using LineTrader.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LineTrader.View
{
    public class Position
    {
        private Model.Oanda.Position model;
        public decimal? Current { get; }
        public decimal? CurrencyRate { get; }
        public long Id { get { return model.id; } }
        public int Units { get { return model.units; } }
        public Model.OrderSide? Side { get { return Model.OrderSides.ToOrderSide(model.side); } }
        public string Instrument { get { return model.InstrumentName; } }
        public string DateTime { get { return model.DateTime.ToString("yyyy/MM/dd HH:mm:ss"); } }
        public decimal Price { get { return model.price; } }
        public decimal? StopLoss { get { return (model.stopLoss == 0) ? (decimal?) null : model.stopLoss; } }
        public decimal? TakeProfit { get { return (model.takeProfit == 0) ? (decimal?) null : model.takeProfit; } }
        public decimal? StopLossWidth { get { return (StopLoss == 0) ? null : Model.OrderSides.Direction(Side) * (Price - StopLoss); } }
        public decimal? TakeProfitWidth { get { return (TakeProfit == 0) ? null : Model.OrderSides.Direction(Side) * (TakeProfit - Price); } }
        public decimal? StopLossRemain { get { return (StopLoss == 0) ? null : Model.OrderSides.Direction(Side) * (Current - StopLoss); } }
        public decimal? TakeProfitRemain { get { return (TakeProfit == 0) ? null : Model.OrderSides.Direction(Side) * (TakeProfit - Current); } }
        public string RiskRewardRatio { get { return (StopLossWidth == 0) ? null : (TakeProfitWidth / StopLossWidth)?.ToString("0.0000"); } }
        public string RiskAtAccountCurrency { get { return (StopLossWidth * Units * CurrencyRate)?.ToString("0.0000"); } }

        public Position(Model.Oanda.Position model, Model.Oanda.Price price, decimal? rate)
        {
            this.model = model;
            this.Current =
                (model.OrderSide == Model.OrderSide.Buy) ? price?.bid :
                (model.OrderSide == Model.OrderSide.Sell) ? price?.ask : null
            ;
            this.CurrencyRate = rate;
        }
    }

    public class Positions
    {
        public ObservableSortedList<object, Position> Items { get; }
        private Dictionary<long, Position> dict;

        public Positions()
        {
            this.Items = new ObservableSortedList<object, Position>(p => p.Id);
            this.dict = new Dictionary<long, Position>();
        }

        public void Set(IEnumerable<Position> xs)
        {
            this.dict = xs.ToDictionary(x => x.Id);
            this.Items.Set(this.dict.Values);
        }
        
        public Position this[long? id]
        {
            get
            {
                if (id == null || !this.dict.ContainsKey(id ?? 0))
                {
                    return null;
                }
                return this.dict[id ?? 0];
            }
        }
    }
}
