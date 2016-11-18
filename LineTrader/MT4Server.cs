using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace LineTrader.MT4
{
    public class Server : IDisposable
    {
        private Service service;
        private HttpListener listener;

        public Server(Service service)
        {
            this.service = service;
            this.listener = new HttpListener();
            this.listener.Prefixes.Add("http://localhost/");
        }

        public void Start()
        {
            this.listener.Start();
            Task.Run(() =>
            {
                var serializer = new JavaScriptSerializer();
                while (true)
                {
                    try
                    {
                        var context = this.listener.GetContext();
                        var body = GetRequestBody(context.Request);
                        var c = serializer.Deserialize<MT4.Command>(body);
                        //Console.WriteLine("data: {0}", body);
                        var ret = this.service.Apply(c);
                        if (!ret)
                        {
                            context.Response.StatusCode = 412;
                        }
                        context.Response.Close();
                    }
                    catch (HttpListenerException e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }
                }
            });
        }

        private static string GetRequestBody(HttpListenerRequest request)
        {
            using (var s = request.InputStream)
            {
                using (var reader = new StreamReader(s))
                {
                    var body = reader.ReadToEnd();
                    return body?.Replace("\0", "");
                }
            }
        }

        public void Dispose()
        {
            this.listener.Close();
        }
    }
}
