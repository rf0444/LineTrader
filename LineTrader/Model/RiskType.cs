using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace LineTrader.Model
{
    public class RiskType
    {
        public string Key { get; }
        public string Name { get; }
        public Func<Oanda.Account, decimal, decimal> Risk { get; }
        public Func<Oanda.Account, string> Unit { get; }

        private RiskType(string key, string name, Func<Oanda.Account, decimal, decimal> risk, Func<Oanda.Account, string> unit)
        {
            this.Key = key;
            this.Name = name;
            this.Risk = risk;
            this.Unit = unit;
        }

        public static readonly RiskType FreeMarginRatio = new RiskType(
            "FreeMarginRatio",
            "余剰証拠金",
            (a, v) => a?.marginAvail * v / 100 ?? 0,
            _ => "%"
        );
        public static readonly RiskType BalanceRatio = new RiskType(
            "BalanceRatio",
            "残高",
            (a, v) => a?.balance * v / 100 ?? 0,
            _ => "%"
        );
        public static readonly RiskType Fixed = new RiskType("Fixed", "固定金額", (a, v) => v, a => a?.accountCurrency);

        public static readonly RiskType[] Values = new RiskType[] { FreeMarginRatio, BalanceRatio, Fixed };
        public static readonly ReadOnlyDictionary<string, RiskType> Items = new ReadOnlyDictionary<string, RiskType>(Values.ToDictionary(x => x.Key));
        public static RiskType Get(string key)
        {
            return Items.ContainsKey(key) ? Items[key] : FreeMarginRatio;
        }
    }
}
