using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TangibleTable.Comm
{
    /// <summary>后台线程收 UDP 文本，主线程每帧调用 DrainTo 派发，避免跨线程碰 Unity API。</summary>
    public sealed class UdpJsonReceiver : IDisposable
    {
        private readonly UdpClient _client;
        private readonly Thread _thread;
        private readonly ConcurrentQueue<string> _queue = new();
        private volatile bool _running = true;

        public UdpJsonReceiver(int port)
        {
            _client = new UdpClient(port);
            _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = $"UdpJsonReceiver:{port}" };
            _thread.Start();
        }

        private void ReceiveLoop()
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    var bytes = _client.Receive(ref remote);
                    _queue.Enqueue(Encoding.UTF8.GetString(bytes));
                }
                catch (SocketException) { /* Dispose 时 Close 会打断阻塞，属预期 */ }
                catch (ObjectDisposedException) { return; }
            }
        }

        public void DrainTo(Action<string> handle)
        {
            while (_queue.TryDequeue(out var json)) handle(json);
        }

        public void Dispose()
        {
            _running = false;
            _client.Close();
        }
    }
}
