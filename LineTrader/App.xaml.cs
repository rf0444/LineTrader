using System.Windows;

namespace LineTrader
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var settings = LineTrader.Properties.Settings.Default;
            if (settings.AccountToken == "" || settings.AccountId == 0)
            {
                var win = new View.AccountSettingWindow(true);
                win.AccountUpdated += _ => Dispatcher.Invoke(() => StartApplication(settings));
                win.Show();
            }
            else
            {
                StartApplication(settings);
            }
        }

        private void StartApplication(LineTrader.Properties.Settings settings)
        {
            var restClient = new Model.Oanda.RestClient(settings.Practice, settings.AccountToken, settings.AccountId);
            var service = new Model.Service(restClient, settings.Instruments.Split(','));
            var win = new View.MainWindow(service, settings.DefalutSizeValue);
            win.Show();
            var mt4Server = new MT4Server(service);
            mt4Server.Start();
        }
    }
}
