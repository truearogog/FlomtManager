using System.Buffers;
using System.Diagnostics;

namespace FlomtManager.Modbus;

public abstract class ModbusProtocol : IModbusProtocol
{
    private const ushort MAX_REQUEST_SIZE_BYTES = 250;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public abstract bool IsOpen { get; }

    public abstract ValueTask DisposeAsync();

    public abstract ValueTask OpenAsync(CancellationToken cancellationToken);
    public abstract ValueTask CloseAsync(CancellationToken cancellationToken);

    public async Task<ushort[]> ReadRegistersAsync(byte slaveId, ushort start, ushort count,
        Action<int, int> progressHandler = null, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadRegistersBytesAsync(slaveId, start, count, progressHandler, cancellationToken);
        var registers = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            registers[i] = bytes[2 * i + 1];
            registers[i] <<= 8;
            registers[i] += bytes[2 * i];
        }
        return registers;
    }

    public async Task<byte[]> ReadRegistersBytesAsync(byte slaveId, ushort start, ushort count,
        Action<int, int> progressHandler = null, CancellationToken cancellationToken = default)
    {
        int byteCount = count * 2, // total number of bytes to read
            left = byteCount; // how many bytes left to read
        var result = new byte[byteCount];
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            var current = start; // current position in registers
            while (left > 0 && !cancellationToken.IsCancellationRequested)
            {
                var toRead = int.Min(left, MAX_REQUEST_SIZE_BYTES); // how many bytes to read
                var read = await ReadRegistersBytesAsyncInternal(slaveId, current, (ushort)(toRead / 2), cancellationToken);
                read.CopyTo(result, (current - start) * 2); // copy read bytes to result array (* 2 because each register is 2 bytes)
                left -= toRead;
                current += (ushort)(toRead / 2);
                progressHandler?.Invoke(current, count);

                if (left > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(2), cancellationToken);
                }
            }
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
        finally
        {
            if (_semaphore.CurrentCount == 0)
            {
                _semaphore.Release();
            }
        }
        return result;
    }

    private async Task<byte[]> ReadRegistersBytesAsyncInternal(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        var message = new byte[8];
        BuildMessage(slaveId, 3, start, count, ref message);
        await SendAsync(message, cancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken);
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

        var crc = CRCHelper.GetCRCBytes(message.AsSpan(0, message.Length - 2));
        message[^2] = crc[0];
        message[^1] = crc[1];
    }

    private static bool CheckResponse(byte[] response)
    {
        var crc = CRCHelper.GetCRCBytes(response.AsSpan(0, response.Length - 2));
        return crc[0] == response[^2] && crc[1] == response[^1];
    }

    protected abstract Task SendAsync(byte[] message, CancellationToken cancellationToken = default);
    protected abstract Task<byte[]> ReceiveAsync(int count, CancellationToken cancellationToken = default);
}
