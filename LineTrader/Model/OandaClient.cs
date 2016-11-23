using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace LineTrader.Model.Oanda
{
    public class RestClient : IDisposable
    {
        private HttpClient http;
        private long accountId;
        private string hostBase;

        public RestClient(bool isPractice, string token, long accountId)
        {
            this.accountId = accountId;
            this.http = new HttpClient();
            this.http.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            this.hostBase = isPractice ? "fxpractice.oanda.com" : "fxtrade.oanda.com";
        }

        public Task<Account> GetAccount()
        {
            var res = http.GetStringAsync("https://api-" + this.hostBase + "/v1/accounts/" + this.accountId);
            return res.ContinueWith(s =>
            {
                return new JavaScriptSerializer().Deserialize<Account>(s.Result);
            });
        }

        public IObservable<Price> GetPriceStream(string[] instruments)
        {
            if (instruments.Length == 0)
            {
                return Observable.Empty<Price>();
            }
            var instrument = string.Join("%2C", instruments.Select(s => s.Replace("/", "_")));
            var url = "https://stream-" + this.hostBase + "/v1/prices?accountId=" + this.accountId + "&instruments=" + instrument;
            var response = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            return StreamResponse(response, str =>
            {
                return new JavaScriptSerializer().Deserialize<PriceStreamElement>(str).tick;
            });
        }

        public Task<string> SendOrder(Order order)
        {
            var res = http.PostAsync("https://api-" + this.hostBase + "/v1/accounts/" + this.accountId + "/orders", new FormUrlEncodedContent(order.ToDictionary()));
            return res.ContinueWith(s => s.Result.Content.ReadAsStringAsync().Result);
        }

        public IObservable<Transaction> GetEventStream()
        {
            var url = "https://stream-" + this.hostBase + "/v1/events?accountId=" + this.accountId;
            var response = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            return StreamResponse(response, str =>
            {
                return new JavaScriptSerializer().Deserialize<EventStreamElement>(str).transaction;
            });
        }

        public Task<Position[]> GetPositions()
        {
            var res = http.GetStringAsync("https://api-" + this.hostBase + "/v1/accounts/" + this.accountId + "/trades");
            return res.ContinueWith(s =>
            {
                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<Positions>(s.Result).trades;
            });
        }

        public Task ClosePosition(long id)
        {
            return http.DeleteAsync("https://api-" + this.hostBase + "/v1/accounts/" + this.accountId + "/trades/" + id);
        }

        public void Dispose()
        {
            this.http.Dispose();
        }

        private static IObservable<T> StreamResponse<T>(Task<HttpResponseMessage> response, Func<string, T> f)
        {
            var subject = new Subject<T>();
            response.ContinueWith(s =>
            {
                var result = s.Result;
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    var stream = result.Content.ReadAsStreamAsync().Result;
                    var reader = new StreamReader(stream);
                    while (true)
                    {
                        string line;
                        try
                        {
                            line = reader.ReadLine();
                        }
                        catch (IOException)
                        {
                            break;
                        }
                        var x = f(line);
                        if (x != null)
                        {
                            subject.OnNext(x);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("{0} - {1} {2}", result.RequestMessage.RequestUri, result.StatusCode, result.Content.ReadAsStringAsync().Result);
                    //subject.OnError(new Exception());
                }
            });
            return subject;
        }
    }
}