using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Web.Script.Serialization;

namespace LineTrader
{

    public class MT4Server : IDisposable
    {
        private ServiceHost host;

        public MT4Server(Model.Service service)
        {
            this.host = new ServiceHost(new WebApp(service), new Uri("http://localhost/"));
            this.host.Description.Behaviors.Find<ServiceDebugBehavior>().HttpHelpPageEnabled = false;
            this.host.AddServiceEndpoint(typeof(WebApp), new WebHttpBinding(), "").EndpointBehaviors.Add(new WebHttpBehavior());
        }

        public void Start()
        {
            this.host.Open();
        }
        
        public void Dispose()
        {
            this.host.Close();
        }
    }

    [ServiceContract]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class WebApp
    {
        private Model.Service service;

        public WebApp(Model.Service service)
        {
            this.service = service;
        }

        [OperationContract]
        [WebInvoke(UriTemplate = "")]
        public void Apply(Stream input)
        {
            var reader = new StreamReader(input);
            var body = reader.ReadToEnd()?.Replace("\0", "");
            reader.Dispose();
            var command = new JavaScriptSerializer().Deserialize<Model.MT4.Command>(body);
            WebOperationContext.Current.OutgoingResponse.StatusCode = this.service.Apply(command);
        }
    }
}
