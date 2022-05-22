using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreliumSocketAPI
{
    public class TCPServer
    {
        public event Action<CoreliumSocket> Accepted;
        public event Action<CoreliumSocket, byte[]> Received;
        public event Action<CoreliumSocket, string> NameReceived;

        private readonly string ip;
        private readonly int port;
        private readonly Socket socket;
        private bool isRunning = false;
        private int bufferSize;

        private readonly List<CoreliumSocket> sockets;
        public TCPServer(int bufferSize, string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            this.bufferSize = bufferSize;

            var point = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new Socket(point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(point);

            sockets = new();
        }

        public List<CoreliumSocket> GetSockets() => sockets;

        public string GetIP() => ip;
        public int GetPort() => port;

        public CoreliumSocket GetSocket(string name) => 
            sockets.FirstOrDefault(x => x.Name == name);

        public TCPServer Start()
        {
            isRunning = true;
            socket.Listen(0);

            socket.BeginAccept(OnAccept, null);

            return this;
        }

        public TCPServer SetTimeout(int interval)
        {
            sockets.ForEach(x => x.SetTimeout(interval));
            socket.ReceiveTimeout = interval;
            socket.SendTimeout = interval;
            return this;
        }
        
        private void OnAccept(IAsyncResult result)
        {
            if (!isRunning)
                return;
            var s = new CoreliumSocket(socket.EndAccept(result), bufferSize)
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
            sockets.Add(s);

            Accepted?.Invoke(s);

            socket.BeginAccept(OnAccept, null);
        }

        public void Stop()
        {
            sockets.ForEach(x => x.Close());
            sockets.Clear();
            isRunning = false;
            GC.Collect();
        }
    }
}
