using System.Threading.Tasks;
using System.Windows;

namespace LineTrader.View
{
    /// <summary>
    /// AccountSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AccountSettingWindow : Window
    {
        private bool isInitSetting;
        private Task accountTask;

        public delegate void AccountUpdatedHandler(Model.Oanda.Account account, Model.Oanda.RestClient client);
        public event AccountUpdatedHandler AccountUpdated;

        public AccountSettingWindow(bool isInitSetting)
        {
            InitializeComponent();

            this.isInitSetting = isInitSetting;
            var settings = LineTrader.Properties.Settings.Default;
            this.checkbox_Practice.IsChecked = settings.Practice;
            this.textBox_AccountId.Text = settings.AccountId.ToString();
            this.passwordBox_AccessToken.Password = settings.AccountToken;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            var isPractice = this.checkbox_Practice.IsChecked ?? true;
            var accountIdStr = this.textBox_AccountId.Text;
            var token = this.passwordBox_AccessToken.Password;
            this.TriggerInputEnable(false);
            this.label_Message.DataContext = "アカウントを確認しています...";
            long accountId;
            if (!long.TryParse(accountIdStr, out accountId))
            {
                this.label_Message.DataContext = "アカウントIDは数字で入力してください。";
                this.TriggerInputEnable(true);
                return;
            }
            var client = new Model.Oanda.RestClient(isPractice, token, accountId);
            this.accountTask = client.GetAccount().ContinueWith(s =>
            {
                if (s.IsFaulted || s.IsCanceled)
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.label_Message.DataContext = "アカウントの確認に失敗しました。アカウントID、パーソナルアクセストークンを再度確認してください。";
                        this.TriggerInputEnable(true);
                    });
                }
                else
                {
                    var settings = LineTrader.Properties.Settings.Default;
                    settings.Practice = isPractice;
                    settings.AccountId = accountId;
                    settings.AccountToken = token;
                    settings.Save();
                    this.AccountUpdated?.Invoke(s.Result, client);
                    Dispatcher.Invoke(() => this.Close());
                }
            });
        }

        private void TriggerInputEnable(bool isEneble)
        {
            this.checkbox_Practice.IsEnabled = isEneble;
            this.textBox_AccountId.IsEnabled = isEneble;
            this.passwordBox_AccessToken.IsEnabled = isEneble;
            this.button_OK.IsEnabled = isEneble;
            this.button_Cancel.IsEnabled = isEneble;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (this.isInitSetting)
            {
                App.Current.Shutdown();
            }
        }
    }
}
