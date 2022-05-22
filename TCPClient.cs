using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CoreliumSocketAPI
{
    public class TCPClient
    {
        public event Action Connected;
        private readonly CoreliumSocket server;
        private bool isConnected = false;
        public TCPClient(string name, int bufferSize, string ip, int port)
        {
            var point = new IPEndPoint(IPAddress.Parse(ip), port);
            server = new CoreliumSocket(bufferSize, SType.TCP, ip, port);
            server.socket.BeginConnect(point, (a) =>
            {
                server.socket.EndConnect(a);
                server.Send("cname" + name);
                Connected?.Invoke();
                isConnected = true;
            }, null);
        }

        public void Disconnect() =>
            server.StartReceive().socket.BeginDisconnect(false, (a) => server.socket.EndDisconnect(a), null);

        public bool IsConnected() =>
            isConnected;

        public CoreliumSocket GetServer() => server;
    }
}
