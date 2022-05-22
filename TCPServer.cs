using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreliumSocketAPI
{
    public class TCPServer : ISocketService
    {
        public event Action<CoreliumSocket> Accepted;
        public event Action<CoreliumSocket, byte[]> Received;
        public event Action<CoreliumSocket, string> NameReceived;
        public event Action<CoreliumSocket> Disconnected;

        private readonly string ip;
        private readonly int port;
        private readonly CoreliumSocket socket;
        private bool isRunning = false;
        private int bufferSize;

        private readonly List<CoreliumSocket> sockets;
        public TCPServer(int bufferSize, string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            this.bufferSize = bufferSize;

            var point = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new CoreliumSocket(bufferSize, SType.TCP, ip, port);
            socket.socket.Bind(point);

            sockets = new();
        }

        public List<CoreliumSocket> GetSockets() => sockets;

        public string GetIP() => ip;
        public int GetPort() => port;

        public CoreliumSocket GetSocket(string name) => 
            sockets.FirstOrDefault(x => x.Name == name);

        public ISocketService Start()
        {
            isRunning = true;
            socket.socket.Listen(0);

            socket.socket.BeginAccept(OnAccept, null);
            return this;
        }

        public void Send(byte[] buffer) => sockets.ForEach(x => x.Send(buffer));
        public void Send(string message) => sockets.ForEach(x => x.Send(message));

        public TCPServer SetTimeout(int interval)
        {
            sockets.ForEach(x => x.SetTimeout(interval));
            socket.socket.ReceiveTimeout = interval;
            socket.socket.SendTimeout = interval;
            return this;
        }
        
        private void OnAccept(IAsyncResult result)
        {
            if (!isRunning)
                return;
            var s = new CoreliumSocket(socket.socket.EndAccept(result), bufferSize)
                .SetBufferSize(bufferSize)
                .StartReceive();
            s.Received += (a, b) =>
            {
                string data = Encoding.UTF8.GetString(a).Substring(0, b);
                if (data.StartsWith("cname"))
                {
                    s.SetName(data.Substring(5));
                    NameReceived?.Invoke(s, data.Substring(5));
                }
                else
                {
                    byte[] ne = new byte[b];
                    for (int i = 0; i < b; i++)
                        ne[i] = a[i];
                    Received?.Invoke(s, ne);
                }
            };
            s.Disconnected += () => Disconnected?.Invoke(s);
            sockets.Add(s);

            Accepted?.Invoke(s);

            socket.socket.BeginAccept(OnAccept, null);
        }

        public CoreliumSocket GetServer() => socket;

        public bool IsConnected() =>
            isRunning;

        public void Stop()
        {
            sockets.ForEach(x => x.Close());
            sockets.Clear();
            isRunning = false;
            GC.Collect();
        }
    }
}
