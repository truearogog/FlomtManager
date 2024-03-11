using FlomtManager.Core.Constants;
using FlomtManager.Core.Enums;
using ReactiveUI;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO.Ports;

namespace FlomtManager.MemoryReader.ViewModels
{
    public class FormViewModel : ViewModelBase
    {
        private string? _directory;
        [Required]
        public string? Directory
        {
            get => _directory;
            set => this.RaiseAndSetIfChanged(ref _directory, value);
        }

        private string? _fileName;
        public string? FileName
        {
            get => _fileName;
            set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        private int _start = 0;
        [Range(0, DeviceConstants.MEMORY_SIZE_BYTES - 1), DefaultValue(0)]
        public int Start
        {
            get => _start;
            set => this.RaiseAndSetIfChanged(ref _start, value);
        }

        private int _count = (int)DeviceConstants.MEMORY_SIZE_BYTES;
        [Range(2, DeviceConstants.MEMORY_SIZE_BYTES), DefaultValue(DeviceConstants.MEMORY_SIZE_BYTES)]
        public int Count
        {
            get => _count;
            set => this.RaiseAndSetIfChanged(ref _count, value);
        }

        private byte _dataRecordLength = 64;
        [Range(1, 255), DefaultValue((byte)64)]
        public byte DataRecordLength
        {
            get => _dataRecordLength;
            set => this.RaiseAndSetIfChanged(ref _dataRecordLength, value);
        }

        private ConnectionType _connectionType = ConnectionType.Serial;
        public ConnectionType ConnectionType
        {
            get => _connectionType;
            set => this.RaiseAndSetIfChanged(ref _connectionType, value);
        }

        private byte _slaveId = 0;
        [Range(1, 255)]
        public byte SlaveId
        {
            get => _slaveId;
            set => this.RaiseAndSetIfChanged(ref _slaveId, value);
        }

        private string _portName = string.Empty;
        public string PortName
        {
            get => _portName;
            set => this.RaiseAndSetIfChanged(ref _portName, value);
        }

        private int _baudRate = 57600;
        public int BaudRate
        {
            get => _baudRate;
            set => this.RaiseAndSetIfChanged(ref _baudRate, value);
        }

        private Parity _parity = Parity.None;
        public Parity Parity
        {
            get => _parity;
            set => this.RaiseAndSetIfChanged(ref _parity, value);
        }

        private int _dataBits = 8;
        public int DataBits
        {
            get => _dataBits;
            set => this.RaiseAndSetIfChanged(ref _dataBits, value);
        }

        private StopBits _stopBits = StopBits.One;
        public StopBits StopBits
        {
            get => _stopBits;
            set => this.RaiseAndSetIfChanged(ref _stopBits, value);
        }

        private string? _ipAddress;
        public string? IpAddress
        {
            get => _ipAddress;
            set => this.RaiseAndSetIfChanged(ref _ipAddress, value);
        }

        private int _port;
        public int Port
        {
            get => _port;
            set => this.RaiseAndSetIfChanged(ref _port, value);
        }

        public DateTime DateTime { get; set; }
        public int Number { get; set; } = 1;
    }
}
