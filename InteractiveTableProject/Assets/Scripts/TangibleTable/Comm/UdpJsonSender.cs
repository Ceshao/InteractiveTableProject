using System;
using System.Net.Sockets;
using System.Text;

namespace TangibleTable.Comm
{
    /// <summary>本机回环 UDP 文本发送器。</summary>
    public sealed class UdpJsonSender : IDisposable
    {
        private readonly UdpClient _client;

        public UdpJsonSender(string host, int port)
        {
            _client = new UdpClient();
            _client.Connect(host, port);
        }

        public void Send(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            _client.Send(bytes, bytes.Length);
        }

        public void Dispose() => _client.Close();
    }
}
