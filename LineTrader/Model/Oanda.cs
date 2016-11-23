using System;
using System.Collections.Generic;

namespace LineTrader.Model.Oanda
{
    public class Account
    {
        public long accountId { get; set; }
        public string accountName { get; set; }
        public decimal balance { get; set; }
        public decimal unrealizedPl { get; set; }
        public decimal realizedPl { get; set; }
        public decimal marginUsed { get; set; }
        public decimal marginAvail { get; set; }
        public int openTrades { get; set; }
        public int openOrders { get; set; }
        public decimal marginRate { get; set; }
        public string accountCurrency { get; set; }
    }

    public class PriceStreamElement
    {
        public Price tick { get; set; }
    }

    public class Price
    {
        public string instrument { get; set; }
        public string InstrumentName { get { return instrument.Replace("_", "/"); } }
        public decimal ask { get; set; }
        public decimal bid { get; set; }
        public string time { get; set; }
        public DateTime DateTime { get { return Util.ToDateTime(time); } }
        public decimal spread { get { return ask - bid; } }
    }

    public class EventStreamElement
    {
        public Transaction transaction { get; set; }
    }

    public class Transaction
    {
        public long id { get; set; }
        public long accountId { get; set; }
        public string time { get; set; }
        public DateTime DateTime {  get { return Util.ToDateTime(time);  } }
        public string type { get; set; }
        public string instrument { get; set; }
        public string side { get; set; }
        public int? units { get; set; }
        public decimal? price { get; set; }
    }

    public class Order
    {
        private string instrument;
        public string Instrument
        {
            get { return this.instrument; }
            set { this.instrument = value.Replace("/", "_"); }
        }
        public int Units { get; set; }
        private string side;
        public OrderSide? Side
        {
            get { return this.side.ToOrderSide(); }
            set { this.side = value?.ToString().ToLower(); }
        }
        public string Type { get; set; } = "market";
        public string Expiry { get; set; }
        public decimal? Price { get; set; }
        public decimal? LowerBound { get; set; }
        public decimal? UpperBound { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? TrailingStop { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var ret = new Dictionary<string, string>
            {
                { "instrument", this.instrument },
                { "units", this.Units.ToString() },
                { "side", this.side },
                { "type", this.Type },
            };
            if (this.Expiry != null)
            {
                ret.Add("expiry", this.Expiry);
            }
            if (this.Price != null)
            {
                ret.Add("price", this.Price.ToString());
            }
            if (this.LowerBound != null)
            {
                ret.Add("lowerBound", this.LowerBound.ToString());
            }
            if (this.UpperBound != null)
            {
                ret.Add("upperBound", this.UpperBound.ToString());
            }
            if (this.StopLoss != null)
            {
                ret.Add("stopLoss", this.StopLoss.ToString());
            }
            if (this.TakeProfit != null)
            {
                ret.Add("takeProfit", this.TakeProfit.ToString());
            }
            if (this.TrailingStop != null)
            {
                ret.Add("trailingStop", this.TrailingStop.ToString());
            }
            return ret;
        }
    }
    
    public class Positions
    {
        public Position[] trades { get; set; }
    }

    public class Position
    {
        public long id { get; set; }
        public int units { get; set; }
        public string side { get; set; }
        public OrderSide? OrderSide { get { return side.ToOrderSide(); } }
        public string instrument { get; set; }
        public string InstrumentName { get { return instrument.Replace("_", "/"); } }
        public string time { get; set; }
        public DateTime DateTime { get { return Util.ToDateTime(time); } }
        public decimal price { get; set; }
        public decimal stopLoss { get; set; }
        public decimal takeProfit { get; set; }
        public decimal trailingStop { get; set; }
        public decimal trailingAmount { get; set; }
    }

    public static class Util
    {
        public static DateTime ToDateTime(string str)
        {
            if (str == null)
            {
                return new DateTime(0);
            }
            return DateTime.Parse(str);
        }
    }
}
