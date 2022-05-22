using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CoreliumSocketAPI
{
    public class CoreliumSocket
    {
        public event Action<byte[], int> Received;
        public event Action Disconnected;

        public string Name { get; set; }
        private int bufferSize = 4096;
        private byte[] buffer = new byte[4096];
        public readonly Socket socket;
        public CoreliumSocket(Socket socket, int bufferSize)
        {
            this.bufferSize = bufferSize;
            buffer = new byte[bufferSize];

            this.socket = socket;
        }

        public CoreliumSocket(int bufferSize, SType type, string ip, int port)
        {
            this.bufferSize = bufferSize;
            buffer = new byte[bufferSize];

            var info = GetSocketInfo(type);
            socket = new Socket(new IPEndPoint(IPAddress.Parse(ip), port).AddressFamily, info.Type, info.Protocol);
        }

        public CoreliumSocket SetTimeout(int interval)
        {
            socket.ReceiveTimeout = socket.SendTimeout = interval;
            return this;
        }

        public void Close()
        {
            socket.Close();
        }

        public CoreliumSocket SetBufferSize(int bufferSize)
        {
            buffer = new byte[bufferSize];
            this.bufferSize = bufferSize;

            return this;
        }

        public CoreliumSocket SetName(string name)
        {
            Name = name;
            return this;
        }

        public CoreliumSocket StartReceive()
        {
            socket.BeginReceive(buffer, 0, bufferSize, SocketFlags.None, OnReceive, null);
            return this;
        }

        public void Send(byte[] buffer) =>
            socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, (a) => 
            socket.EndSend(a), null);
        public void Send(string text) =>
            Send(Encoding.UTF8.GetBytes(text));

        private void OnReceive(IAsyncResult result)
        {
            if (!socket.Connected)
            {
                Disconnected?.Invoke();
                return;
            }
            try
            {
                int received = socket.EndReceive(result);
                Received?.Invoke(buffer, received);
                buffer = new byte[buffer.Length];
                StartReceive();
            }
            catch (Exception)
            {
                buffer = new byte[buffer.Length];
                Disconnected?.Invoke();
            }
        }

        private struct SocketInfo
        {
            public SocketType Type { get; set; }
            public ProtocolType Protocol { get; set; }
        }

        private static SocketInfo GetSocketInfo(SType type) => type switch
        {
            SType.TCP => new()
            {
                Type = SocketType.Stream,
                Protocol = ProtocolType.Tcp
            },
            SType.UDP => new()
            {
                Type = SocketType.Dgram,
                Protocol = ProtocolType.Udp
            },
            _ => default
        };
    }

    public enum SType
    {
        TCP, UDP
    }
}
