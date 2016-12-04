namespace LineTrader.View
{
    public class RiskSetting
    {
        public Model.RiskType Type { get; set; }
        public decimal Value { get; set; }
        public bool ManualClose { get; set; }

        public static RiskSetting Default
        {
            get
            {
                var settings = LineTrader.Properties.Settings.Default;
                return new RiskSetting
                {
                    Type = Model.RiskType.Get(settings.DefaultSizeType),
                    Value = settings.DefalutSizeValue,
                    ManualClose = settings.ManualClose,
                };
            }
        }
    }
}
