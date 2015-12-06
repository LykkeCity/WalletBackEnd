using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common.HttpRemoteRequests
{
    public class HttpRequestClient 
    {
        private readonly IPEndPoint _ipEndPoint;

        public HttpRequestClient(IPEndPoint ipEndPoint)
        {
            _ipEndPoint = ipEndPoint;
        }


        public async Task<string> Request(string data)
        {
            var oWebRequest =
                (HttpWebRequest) WebRequest.Create("http://" + _ipEndPoint.Address + ":" + _ipEndPoint.Port);
            oWebRequest.Method = "POST";
            oWebRequest.ContentType = "text/plain";

            var stream = oWebRequest.GetRequestStream();
            var dataToSend = Encoding.UTF8.GetBytes(data);
            stream.Write(dataToSend, 0, dataToSend.Length);

            var oWebResponse = await oWebRequest.GetResponseAsync();
            var receiveStream = oWebResponse.GetResponseStream();

            try
            {
                if (receiveStream == null)
                    throw new Exception("ReceiveStream == null");

                var ms = new MemoryStream();
                receiveStream.CopyTo(ms);
                var array = ms.ToArray();

                if (array.Length > 0)
                  return Encoding.UTF8.GetString(ms.ToArray());

            }
            catch (Exception)
            {
                return string.Empty;
            }
            return string.Empty;
        }
    }
}
