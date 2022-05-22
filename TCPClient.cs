using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CoreliumSocketAPI
{
    public class TCPClient : ISocketService
    {
        public event Action Connected;

        private readonly string name;
        private readonly string ip;
        private readonly int port;

        private readonly CoreliumSocket server;
        private bool isConnected = false;
        public TCPClient(string name, int bufferSize, string ip, int port)
        {
            this.name = name;
            this.ip = ip;
            this.port = port;

            server = new CoreliumSocket(bufferSize, SType.TCP, ip, port);
        }

        public ISocketService Start()
        {
            var point = new IPEndPoint(IPAddress.Parse(ip), port);
            server.socket.BeginConnect(point, (a) =>
            {
                server.socket.EndConnect(a);
                server.Send("cname" + name);
                Connected?.Invoke();
                isConnected = true;
            }, null);
            return this;
        }

        public void Stop() =>
            server.StartReceive().socket.BeginDisconnect(false, (a) => server.socket.EndDisconnect(a), null);

        public bool IsConnected() =>
            isConnected;

        public CoreliumSocket GetServer() => server;
    }
}
