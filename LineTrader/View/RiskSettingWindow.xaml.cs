using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LineTrader.View
{
    /// <summary>
    /// RiskSetting.xaml の相互作用ロジック
    /// </summary>
    public partial class RiskSettingWindow : Window
    {
        public delegate void RiskUpdatedHandler(RiskSetting setting);
        public event RiskUpdatedHandler RiskUpdated;

        public RiskSettingWindow(RiskSetting current, Model.Oanda.Account account)
        {
            InitializeComponent();

            var riskTypes = Model.RiskType.Values.Select(x =>
            {
                var text = (x == Model.RiskType.Fixed) ? x.Name : string.Format("{0} ({1} {2}) の", x.Name, x.Risk(account, 100), account.accountCurrency);
                return new RiskTypeView(x, text, x.Unit.Invoke(account));
            }).ToDictionary(x => x.Model.Key);
            this.comboBox_riskTypeSelect.ItemsSource = Model.RiskType.Values.Select(x => riskTypes[x.Key]);

            this.comboBox_riskTypeSelect.SelectedItem = (riskTypes.ContainsKey(current.Type.Key))
                ? riskTypes[current.Type.Key]
                : riskTypes[Model.RiskType.FreeMarginRatio.Key]
            ;
            this.textBox_riskValue.Text = current.Value.ToString();
            this.checkbox_ManualClose.IsChecked = current.ManualClose;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            var riskType = this.comboBox_riskTypeSelect.SelectedValue as RiskTypeView;
            decimal riskValue;
            if (!decimal.TryParse(this.textBox_riskValue.Text, out riskValue))
            {
                this.label_Message.DataContext = "許容リスク値が数値ではありません。";
                return;
            }
            var manualClose = this.checkbox_ManualClose.IsChecked ?? true;
            var setting = new RiskSetting
            {
                Type = riskType.Model,
                Value = riskValue,
                ManualClose = manualClose,
            };
            if (this.checkbox_Default.IsChecked ?? false)
            {
                var settings = LineTrader.Properties.Settings.Default;
                settings.DefaultSizeType = setting.Type.Key;
                settings.DefalutSizeValue = setting.Value;
                settings.ManualClose = setting.ManualClose;
                settings.Save();
            }
            this.RiskUpdated?.Invoke(setting);
            Dispatcher.Invoke(() => this.Close());
        }

        private void comboBox_riskTypeSelect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var riskType = this.comboBox_riskTypeSelect.SelectedValue as RiskTypeView;
            this.textBlock_riskUnit.Text = riskType.Unit;
        }
    }

    public class RiskTypeView
    {
        public Model.RiskType Model { get; }
        public string Text { get; }
        public string Unit { get; }

        public RiskTypeView(Model.RiskType model, string text, string unit)
        {
            this.Model = model;
            this.Text = text;
            this.Unit = unit;
        }
    }
}
