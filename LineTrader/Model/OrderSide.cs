namespace LineTrader.Model
{
    public enum OrderSide { Buy, Sell }

    public static class OrderSides
    {
        public static OrderSide? ToOrderSide(this string x)
        {
            switch (x.ToLower())
            {
                case "buy": return OrderSide.Buy;
                case "sell": return OrderSide.Sell;
                default: return null;
            }
        }

        public static int Direction(this OrderSide? x)
        {
            switch (x)
            {
                case OrderSide.Buy: return 1;
                case OrderSide.Sell: return -1;
                default: return 0;
            }
        }

        public static OrderSide? Inv(this OrderSide? x)
        {
            switch (x)
            {
                case OrderSide.Buy: return OrderSide.Sell;
                case OrderSide.Sell: return OrderSide.Buy;
                default: return null;
            }
        }

        public static string PriceKind(this OrderSide? x)
        {
            switch (x)
            {
                case OrderSide.Buy: return "Ask";
                case OrderSide.Sell: return "Bid";
                default: return "";
            }
        }
    }
}
