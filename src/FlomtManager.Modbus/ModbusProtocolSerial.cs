using System.IO.Ports;

namespace FlomtManager.Modbus
{
    public class ModbusProtocolSerial(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) : ModbusProtocolBase
    {
        private SerialPort? _serialPort;
        private readonly string _portName = portName;
        private readonly int _baudRate = baudRate;
        private readonly Parity _parity = parity;
        private readonly int _dataBits = dataBits;
        private readonly StopBits _stopBits = stopBits;

        public override bool IsOpen => _serialPort?.IsOpen ?? false;

        public override void Open()
        {
            _serialPort = new()
            {
                PortName = _portName,
                BaudRate = _baudRate,
                Parity = _parity,
                DataBits = _dataBits,
                StopBits = _stopBits,
                Handshake = Handshake.None,
                WriteTimeout = 500
            };
            _serialPort.Open();
        }

        public override void Close()
        {
            _serialPort?.Close();
        }

        protected override void Send(byte[] message)
        {
            _serialPort?.Write(message, 0, message.Length);
        }

        protected override byte[]? Receive(int count)
        {
            if (_serialPort is null)
            {
                return null;
            }

            var size = count * 2 + 5;
            var recv = new byte[size];
            for (int i = 0; i < size; ++i)
            {
                recv[i] = (byte)_serialPort.ReadByte();
            }

            return recv;
        }

        public override void Dispose()
        {
            Close();
        }
    }
}
