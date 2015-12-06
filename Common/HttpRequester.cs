using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common
{



    public static class HttpRequester
    {
        public enum ContentType
        {
            UrlEncoded, TextXml
        }

        public enum Method
        {
            Get, Post
        }

        private static string GetContentType(ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.TextXml:
                    return "text/xml";
            }
            return "application/x-www-form-urlencoded";
        }

        private static string GetMethod(Method method)
        {
            switch (method)
            {
                case Method.Get:
                    return "GET";
            }
            return "POST";
        }

        /// <summary>
        /// Делаем запрос через POST
        /// </summary>
        /// <param name="url">Адрес, куда будет производиться запрос</param>
        /// <param name="model">Параметры запроса</param>
        /// <param name="beforeRequest">Если есть необходимость заполнить дополнительные поля перед запросом - можно сделать это здесь</param>
        /// <param name="contentType">Типы контента</param>
        /// <param name="method">Метод, используемый при запросе</param>
        /// <param name="encoding">Тип енкодера для кодирования запроса/Ответа</param>
        /// <returns>Результат в виде строки</returns>
        public static async Task<string> RequestAsync(string url, object model, Method method, Action<HttpWebRequest> beforeRequest = null, 
            ContentType contentType = ContentType.UrlEncoded, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            var requestString = UrlUtils.ToUrlParamString(model);
            var oWebRequest = (HttpWebRequest)WebRequest.Create(url);
            oWebRequest.Method = GetMethod(method);
            oWebRequest.ContentType = GetContentType(contentType);

            if (beforeRequest != null)
                beforeRequest(oWebRequest);

            var encodedBytes = encoding.GetBytes(requestString);
            oWebRequest.AllowWriteStreamBuffering = true;

            var requestStream = oWebRequest.GetRequestStream();
            requestStream.Write(encodedBytes, 0, encodedBytes.Length);

            var oWebResponse = await oWebRequest.GetResponseAsync();
            var receiveStream = oWebResponse.GetResponseStream();

            if (receiveStream == null)
                throw new Exception("receiveStream == null");

            var sr = new StreamReader(receiveStream);
            var resultString = sr.ReadToEnd();
            return resultString;
        }

    }

}
