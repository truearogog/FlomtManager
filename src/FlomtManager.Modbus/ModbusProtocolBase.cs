
using System.Buffers;
using System.Net.Http.Headers;

namespace FlomtManager.Modbus
{
    public abstract class ModbusProtocolBase : IModbusProtocol, IDisposable
    {
        private const ushort MAX_REQUEST_SIZE = 250;

        public abstract bool IsOpen { get; }

        public abstract ValueTask OpenAsync(CancellationToken cancellationToken);
        public abstract ValueTask CloseAsync(CancellationToken cancellationToken);

        public async Task<ushort[]> ReadRegistersAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken)
        {
            var bytes = await ReadRegistersBytesAsync(slaveId, start, count, cancellationToken);
            var registers = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                registers[i] = bytes[2 * i + 1];
                registers[i] <<= 8;
                registers[i] += bytes[2 * i];
            }
            return registers;
        }

        public async Task<byte[]> ReadRegistersBytesAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken)
        {
            int byteCount = count * 2, left = byteCount;
            var result = new byte[byteCount];
            ushort current = start;
            while (left > 0 && !cancellationToken.IsCancellationRequested)
            {
                var toRead = int.Min(left, MAX_REQUEST_SIZE);
                var read = await ReadRegistersBytesAsyncInternal(slaveId, current, (ushort)(toRead / 2), cancellationToken);
                read.CopyTo(result, current - start);
                left -= toRead;
                current += (ushort)toRead;
            }
            return result;
        }

        private async Task<byte[]> ReadRegistersBytesAsyncInternal(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken)
        {
            var message = ArrayPool<byte>.Shared.Rent(8);
            BuildMessage(slaveId, 3, start, count, ref message);
            await SendAsync(message, cancellationToken);
            ArrayPool<byte>.Shared.Return(message);
            Thread.Sleep(50);
            var bytes = await ReceiveAsync(count, cancellationToken);
            if (bytes is [.., _, _] && CheckResponse(bytes))
            {
                return bytes.Skip(3).SkipLast(2).ToArray();
            }

            throw new Exception("Wrong message CRC.");
        }

        private static void BuildMessage(byte slaveId, byte type, ushort start, ushort count, ref byte[] message)
        {
            message[0] = slaveId;
            message[1] = type;
            message[2] = (byte)(start >> 8);
            message[3] = (byte)start;
            message[4] = (byte)(count >> 8);
            message[5] = (byte)count;

            var crc = ModbusHelper.GetCRCBytes(message.AsSpan(0, message.Length - 2));
            message[^2] = crc[0];
            message[^1] = crc[1];
        }

        private static bool CheckResponse(byte[] response)
        {
            var crc = ModbusHelper.GetCRCBytes(response.AsSpan(0, response.Length - 2));
            return crc[0] == response[^2] && crc[1] == response[^1];
        }

        protected abstract ValueTask SendAsync(byte[] message, CancellationToken cancellationToken);
        protected abstract ValueTask<byte[]> ReceiveAsync(int count, CancellationToken cancellationToken);

        public abstract void Dispose();
    }
}
