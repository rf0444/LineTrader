using NLog;
using System.Windows;

namespace LineTrader
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            logger.Debug("start app");
            var settings = LineTrader.Properties.Settings.Default;
            if (settings.AccountToken == "" || settings.AccountId == 0)
            {
                logger.Debug("no setting");
                ShowAccountSetting(settings);
                return;
            }
            var restClient = new Model.Oanda.RestClient(settings.Practice, settings.AccountToken, settings.AccountId);
            logger.Debug("getting account");
            restClient.GetAccount().ContinueWith(s =>
            {
                logger.Debug("got account");
                Dispatcher.Invoke(() =>
                {
                    if (s.IsFaulted || s.IsCanceled)
                    {
                        ShowAccountSetting(settings);
                    }
                    else
                    {
                        logger.Debug("start app");
                        StartApplication(settings, restClient);
                    }
                });
            });
        }

        private void StartApplication(LineTrader.Properties.Settings settings, Model.Oanda.RestClient restClient)
        {
            var service = new Model.Service(restClient, settings.Instruments.Split(','));
            var win = new View.MainWindow(service);
            logger.Debug("showing window");
            win.Show();
            logger.Debug("window shown");
            var mt4Server = new MT4Server(service);
            logger.Debug("start mt4 server");
            mt4Server.Start();
            logger.Debug("app started");
        }

        private void ShowAccountSetting(LineTrader.Properties.Settings settings)
        {
            var win = new View.AccountSettingWindow(true);
            win.AccountUpdated += (account, client) => Dispatcher.Invoke(() => StartApplication(settings, client));
            win.Show();
        }
    }
}
