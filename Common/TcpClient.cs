using System;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public interface ITcpClient : IDisposable
    {
        void Connect();

        Task ConnectAsync();

        string ReadLine();
        void WriteLine(string line);

        Task<string> ReadLineAsync();
        Task WriteLineAsync(string line);

        void Write(byte[] data);

        string HostPort { get; }
    }

    public class MyTcpClient : ITcpClient
    {
        private readonly string _host;
        private readonly int _port;
        private readonly Encoding _encoding;
        private readonly TcpClient _tcpClient = new TcpClient();
        private Stream _stream;
        private StreamReader _streamReader;

        public MyTcpClient(string host, int port):this(host, port, Encoding.ASCII)
        {

        }

        public MyTcpClient(string host, int port,  Encoding encoding)
        {
            _host = host;
            _port = port;
            HostPort = _host + ':' + _port;
            _encoding = encoding;
        }

        private byte[] GetLine(string line)
        {
            var result = _encoding.GetBytes(line).ToList();
            result.Add(13);
            result.Add(10);
            return result.ToArray();
        }

        public void Dispose()
        {
            ((IDisposable) _tcpClient).Dispose();
        }

        private void GetStream()
        {
            _stream = _tcpClient.GetStream();
            _streamReader = new StreamReader(_stream);
        }

        public void Connect()
        {
            _tcpClient.Connect(_host, _port);
            GetStream();
        }

        public async Task ConnectAsync()
        {
            await _tcpClient.ConnectAsync(_host, _port);
            GetStream();
        }

        public string ReadLine()
        {
            return _streamReader.ReadLine();
        }

        public void WriteLine(string line)
        {
            var data = GetLine(line);
            _stream.Write(data, 0, data.Length);
        }

        public async Task<string> ReadLineAsync()
        {
            return await _streamReader.ReadLineAsync();
        }

        public async Task WriteLineAsync(string line)
        {
            var data = GetLine(line);
            await _stream.WriteAsync(data, 0, data.Length);
        }

        public void Write(byte[] data)
        {
            _stream.Write(data, 0, data.Length);
        }

        public string HostPort { get; private set; }
    }
}
