namespace FlomtManager.Modbus
{
    public abstract class ModbusProtocolBase : IModbusProtocol, IDisposable
    {
        public abstract bool IsOpen { get; }

        public abstract void Open();
        public abstract void Close();

        public ushort[] ReadRegisters(byte slaveId, ushort start, ushort count, CancellationToken ct)
        {
            var bytes = ReadRegistersBytes(slaveId, start, count, ct);
            var registers = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                registers[i] = bytes[2 * i + 1];
                registers[i] <<= 8;
                registers[i] += bytes[2 * i];
            }
            return registers;
        }

        private const ushort MAX_REQUEST_SIZE = 250;

        public byte[] ReadRegistersBytes(byte slaveId, ushort start, ushort count, CancellationToken ct)
        {
            var byteCount = count * 2;
            var result = new byte[byteCount];
            var left = byteCount;
            ushort current = start;
            while (left > 0 && !ct.IsCancellationRequested)
            {
                var toRead = int.Min(left, MAX_REQUEST_SIZE);
                var read = ReadRegistersBytes(slaveId, current, (ushort)(toRead / 2));
                read.CopyTo(result, current - start);
                left -= toRead;
                current += (ushort)toRead;
            }
            return result;
        }

        private byte[] ReadRegistersBytes(byte slaveId, ushort start, ushort count)
        {
            var message = new byte[8];
            BuildMessage(slaveId, 3, start, count, ref message);
            Send(message);
            Thread.Sleep(50);
            var bytes = Receive(count);
            var result = Array.Empty<byte>();
            if (bytes is not null && CheckResponse(bytes))
            {
                result = bytes.Skip(3).SkipLast(2).ToArray();
            }
            return result;
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

        protected abstract void Send(byte[] message);
        protected abstract byte[]? Receive(int count);

        public abstract void Dispose();
    }
}
