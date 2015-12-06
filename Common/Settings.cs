using System;

namespace Common
{
    public class ClientSocketSetup
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int ReconnectTimeOut { get; set; }

        public static ClientSocketSetup ParseHostPort(string hostPort)
        {
            var hostAndPort = hostPort.Split(':');
            if (hostAndPort.Length != 2)
                throw new Exception("Invalid hostPort : [" + hostPort + "]");

            var result = new ClientSocketSetup { Host = hostAndPort[0], Port = int.Parse(hostAndPort[1]) };
            return result;
        }

        public override string ToString()
        {
            return Host + ":" + Port;
        }
    }

    public class ServerTcpSocketSetup
    {
        public int Port { get; set; }
        public int MaxConnections { get; set; }
    }
}
