using System.Net;
using System.Net.Sockets;

namespace FlomtManager.Modbus
{
    public class ModbusProtocolTcp(string ipAddress, int port) : ModbusProtocolBase
    {
        private readonly IPAddress _ipAddress = IPAddress.Parse(ipAddress);
        private readonly int _port = port;
        private Socket? _socket;

        public override bool IsOpen => _socket?.Connected ?? false;

        public override void Dispose()
        {
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
        }

        public override ValueTask OpenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _socket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            return _socket.ConnectAsync(_ipAddress, _port, cancellationToken);
        }

        public override ValueTask CloseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfSocketIsNull();
            _socket!.Shutdown(SocketShutdown.Both);
            _socket!.Close();
            _socket = null;
            return ValueTask.CompletedTask;
        }

        protected override async ValueTask SendAsync(byte[] message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfSocketIsNull();
            if (_socket!.Poll(TimeSpan.FromMilliseconds(50), SelectMode.SelectWrite))
            {
                await _socket!.SendAsync(message, SocketFlags.None, cancellationToken);
            }
            else
            {
                throw new TimeoutException("Socket send timed out.");
            }
        }

        protected override async ValueTask<byte[]> ReceiveAsync(int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfSocketIsNull();
            int size = count * 2 + 5, left = size;
            var result = new byte[size];
            while (left > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_socket!.Poll(TimeSpan.FromSeconds(1), SelectMode.SelectRead))
                {
                    left -= await _socket!.ReceiveAsync(result.AsMemory(size - left), cancellationToken);
                }
                else
                {
                    throw new TimeoutException("Socket receive timed out.");
                }
            }
            return result;
        }

        private void ThrowIfSocketIsNull()
        {
            if (_socket == null)
            {
                throw new InvalidOperationException("Socket is null.");
            }
        }
    }
}
