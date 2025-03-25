using System.IO.Ports;

namespace FlomtManager.Modbus
{
    public class ModbusProtocolSerial(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) : ModbusProtocol
    {
        private SerialPort _serialPort;

        public override bool IsOpen => _serialPort?.IsOpen ?? false;

        public override async ValueTask DisposeAsync()
        {
            await CloseAsync();
            GC.SuppressFinalize(this);
        }

        public override ValueTask OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serialPort = new SerialPort()
            {
                PortName = portName,
                BaudRate = baudRate,
                Parity = parity,
                DataBits = dataBits,
                StopBits = stopBits,
                ReadTimeout = 1000,
                WriteTimeout = 1000,
            };
            _serialPort.Open();
            return ValueTask.CompletedTask;
        }

        public override ValueTask CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
            return ValueTask.CompletedTask;
        }

        protected override Task SendAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(_serialPort);
            _serialPort!.Write(message, 0, message.Length);
            return Task.CompletedTask;
        }

        protected override Task<byte[]> ReceiveAsync(int count, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(_serialPort);
            return Task.Run(() =>
            {
                int size = count * 2 + 5, left = size;
                var result = new byte[size];
                while (left > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    left -= _serialPort!.Read(result, size - left, left);
                }
                return result;
            }, cancellationToken);
        }
    }
}
