using System.Net.Sockets;
using System.Net;

namespace FlomtManager.Modbus
{
    public class ModbusProtocolTcp(string ipAddress, int port) : ModbusProtocolBase
    {
        private readonly IPAddress _ipAddress = IPAddress.Parse(ipAddress);
        private readonly int _port = port;
        private Socket? _socket;

        public override bool IsOpen => _socket?.Connected ?? false;

        public override void Open()
        {
            var endPoint = new IPEndPoint(_ipAddress, _port);
            _socket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(endPoint);
        }

        public override void Close()
        {
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
        }

        protected override void Send(byte[] message)
        {
            _socket?.Send(message);
        }

        protected override byte[]? Receive(int count)
        {
            if (_socket is null)
            {
                return null;
            }

            var size = count * 2 + 5;
            var recv = new byte[size];
            var totalRecv = 0;
            do
            {
                totalRecv += _socket.Receive(recv, totalRecv, size - totalRecv, SocketFlags.None);
            } while (totalRecv < size);

            return recv;
        }

        public override void Dispose()
        {
            Close();
        }
    }
}
