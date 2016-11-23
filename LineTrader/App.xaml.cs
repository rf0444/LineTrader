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
            // TODO: たまに起動しないことがある
            Console.WriteLine("Start");
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
            Console.WriteLine("create client");
            var restClient = new Model.Oanda.RestClient(settings.Practice, settings.AccountToken, settings.AccountId);
            Console.WriteLine("create service");
            var service = new Model.Service(restClient, settings.Instruments.Split(','));
            Console.WriteLine("create mt4 server");
            var mt4Server = new MT4Server(service);
            Console.WriteLine("create window");
            var win = new View.MainWindow(service, settings.DefalutSizeValue);
            Console.WriteLine("start mt4 server");
            mt4Server.Start();
            Console.WriteLine("start window");
            win.Show();
            Console.WriteLine("Started");
        }
    }
}
