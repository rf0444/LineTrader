using System;
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
                ShowAccountSetting(settings);
                return;
            }
            var restClient = new Model.Oanda.RestClient(settings.Practice, settings.AccountToken, settings.AccountId);
            Console.WriteLine("get account");
            restClient.GetAccount().ContinueWith(s =>
            {
                Console.WriteLine("got account");
                Dispatcher.Invoke(() =>
                {
                    if (s.IsFaulted || s.IsCanceled)
                    {
                        ShowAccountSetting(settings);
                    }
                    else
                    {
                        Console.WriteLine("start app");
                        StartApplication(settings, restClient);
                    }
                });
            });
        }

        private void StartApplication(LineTrader.Properties.Settings settings, Model.Oanda.RestClient restClient)
        {
            var service = new Model.Service(restClient, settings.Instruments.Split(','));
            var win = new View.MainWindow(service);
            Console.WriteLine("show window");
            win.Show();
            var mt4Server = new MT4Server(service);
            Console.WriteLine("start mt4 server");
            mt4Server.Start();
            Console.WriteLine("app started");
        }

        private void ShowAccountSetting(LineTrader.Properties.Settings settings)
        {
            var win = new View.AccountSettingWindow(true);
            win.AccountUpdated += (account, client) => Dispatcher.Invoke(() => StartApplication(settings, client));
            win.Show();
        }
    }
}
