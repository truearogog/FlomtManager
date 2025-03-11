using System.Net;
using System.Net.Sockets;

namespace FlomtManager.Modbus
{
    public class ModbusProtocolTcp(string ipAddress, int port) : ModbusProtocol
    {
        private Socket _socket;

        public override bool IsOpen => _socket?.Connected ?? false;

        public override async ValueTask DisposeAsync()
        {
            await CloseAsync();
            GC.SuppressFinalize(this);
        }

        public override ValueTask OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ipAddress_ = IPAddress.Parse(ipAddress);
            _socket = new Socket(ipAddress_.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            return _socket.ConnectAsync(ipAddress_, port, cancellationToken);
        }

        public override ValueTask CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(_socket);
            _socket!.Shutdown(SocketShutdown.Both);
            _socket!.Close();
            _socket!.Dispose();
            _socket = null;
            return ValueTask.CompletedTask;
        }

        protected override async Task SendAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(_socket);
            await _socket!.SendAsync(message, SocketFlags.None, cancellationToken);
        }

        protected override async Task<byte[]> ReceiveAsync(int count, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(_socket);
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
    }
}
