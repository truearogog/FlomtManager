using System.IO.Ports;

namespace FlomtManager.Modbus
{
    public class ModbusProtocolSerial(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) : ModbusProtocolBase
    {
        private SerialPort _serialPort;
        private readonly string _portName = portName;
        private readonly int _baudRate = baudRate;
        private readonly Parity _parity = parity;
        private readonly int _dataBits = dataBits;
        private readonly StopBits _stopBits = stopBits;

        public override bool IsOpen => _serialPort?.IsOpen ?? false;

        public override void Dispose()
        {
            _serialPort?.Close();
            GC.SuppressFinalize(this);
        }

        public override ValueTask OpenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serialPort = new SerialPort()
            {
                PortName = _portName,
                BaudRate = _baudRate,
                Parity = _parity,
                DataBits = _dataBits,
                StopBits = _stopBits,
                ReadTimeout = 1000,
                WriteTimeout = 1000,
            };
            _serialPort.Open();
            return ValueTask.CompletedTask;
        }

        public override ValueTask CloseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(_serialPort);
            _serialPort!.Close();
            return ValueTask.CompletedTask;
        }

        protected override Task SendAsync(byte[] message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(_serialPort);
            _serialPort!.Write(message, 0, message.Length);
            return Task.CompletedTask;
        }

        protected override Task<byte[]> ReceiveAsync(int count, CancellationToken cancellationToken)
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
