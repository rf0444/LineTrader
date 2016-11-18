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
                // TODO: 設定画面
                settings.AccountId = 0;
                settings.AccountToken = "";
                settings.Save();
                Console.WriteLine("no account settings. configure C:\\Users\\{0}\\AppData\\Local\\LineTrader\\...\\user.config", Environment.UserName);
                this.Shutdown();
                return;
            }
            var restClient = new Oanda.RestClient(settings.Practice, settings.AccountToken, settings.AccountId);
            var service = new Service(restClient, settings.DefalutSizeValue);
            var mt4Server = new MT4.Server(service);
            var win = new MainWindow(service);
            service.Start(settings.Instruments.Split(','));
            mt4Server.Start();
            win.Show();
        }
    }
}
