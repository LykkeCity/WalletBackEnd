using System;
using System.Linq;

namespace Common
{
    public class IpWhiteList
    {
        private readonly string _setupString;

        private string[] _lst;
        
        public IpWhiteList(string setupString)
        {
            _setupString = setupString;
        }

        private static byte[] ParseIp(string data)
        {
            var datas = data.Split(':');

            datas = datas[0].Split('.');

            if (datas.Length!=4)
                throw new Exception("Invalid Ip format: "+data);
            return new[] { byte.Parse(datas[0]), byte.Parse(datas[1]), byte.Parse(datas[2]), byte.Parse(datas[3])};
        }

        private static bool IsIpEquals(byte[] ip1, byte[] ip2)
        {
            return ip1[0] == ip2[0] && ip1[1] == ip2[1] && ip1[2] == ip2[2] && ip1[3] == ip2[3];
        }

        public bool IsIpInList(string ipstring)
        {
            if (_lst == null)
             _lst = _setupString.Split(';');

            var myip = ParseIp(ipstring);

            return _lst.Select(ParseIp).Any(ip => IsIpEquals(myip, ip));
        }
    }
}
