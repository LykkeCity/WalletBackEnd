using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Common.Log;

namespace Common.HttpRemoteRequests
{
    public abstract class HttpRequestServer
    {
        private readonly IPEndPoint _endPoint;
        private readonly string _serviceName;
        private readonly ILog _log;

        protected HttpRequestServer(IPEndPoint endPoint, string serviceName, ILog log)
        {
            _endPoint = endPoint;
            _serviceName = serviceName;
            _log = log;
        }

        private HttpListener _listener;


        protected abstract Task<string> HandleRequest(string data); 

        private async Task HandleHttpContext(HttpListenerContext context)
        {

            try
            {
                var stream = new MemoryStream();
                await context.Request.InputStream.CopyToAsync(stream);
                var data = Encoding.UTF8.GetString(stream.ToArray());
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                var response = await HandleRequest(data);

                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.ContentType = "text/plain";

                if (!string.IsNullOrEmpty(response))
                {
                    var bytes = Encoding.UTF8.GetBytes(response);
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                }
                context.Response.Close();
            }
            catch (Exception ex)
            {
                await _log.WriteError(_serviceName, "HandleHttpContext", "", ex);

            }
        }


        private async Task HandleRequests()
        {
            while (_listener != null)
            {
                try
                {
                    ////blocks until a client has connected to the server
                    var context = await _listener.GetContextAsync();
                    await HandleHttpContext(context);

                }
                catch (Exception exception)
                {

                    await _log.WriteError("DataPipeServerHandler", "", "", exception);
                }
            }

        }

        public void Start()
        {
            _listener = new HttpListener();
            var prefix = "http://" + _endPoint.Address + ":" + _endPoint.Port + "/";
            _listener.Prefixes.Add(prefix);
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            _listener.Start();

            Task.Run(async () => { await HandleRequests(); });
        }
    }
}
