using FlomtManager.Core.Constants;
using FlomtManager.Core.Enums;
using FlomtManager.Modbus;
using HexIO;
using ReactiveUI;
using Serilog;
using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FlomtManager.MemoryReader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public FormViewModel Form { get; set; } = new();

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        private IModbusProtocol? _modbusProtocol = null;
        public IModbusProtocol? ModbusProtocol
        {
            get => _modbusProtocol;
            set => this.RaiseAndSetIfChanged(ref _modbusProtocol, value);
        }

        private bool _reading = false;
        public bool Reading
        {
            get => _reading;
            set => this.RaiseAndSetIfChanged(ref _reading, value);
        }

        private int _maxProgress;
        public int MaxProgress
        {
            get => _maxProgress;
            set => this.RaiseAndSetIfChanged(ref _maxProgress, value);
        }

        private int _currentProgress;
        public int CurrentProgress
        {
            get => _currentProgress;
            set => this.RaiseAndSetIfChanged(ref _currentProgress, value);
        }

        public ObservableCollection<ConnectionType> ConnectionTypes { get; set; } = new(Enum.GetValues<ConnectionType>());
        public ObservableCollection<string> PortNames { get; set; } = [];
        public ObservableCollection<int> BaudRates { get; set; } = [110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000];
        public ObservableCollection<Parity> Parities { get; set; } = new(Enum.GetValues<Parity>());
        public ObservableCollection<int> DataBits { get; set; } = [5, 6, 7, 8];
        public ObservableCollection<StopBits> StopBits { get; set; } = new(Enum.GetValues<StopBits>());

        private CancellationTokenSource? _cancellationTokenSource;

        public EventHandler? DirectoryRequested;

        public MainWindowViewModel()
        {
            RefreshPortNames();
        }

        public void RequestDirectory()
        {
            DirectoryRequested?.Invoke(this, EventArgs.Empty);
        }

        public void OpenConnection()
        {
            ErrorMessage = null;
            try
            {
                ModbusProtocol = Form.ConnectionType switch
                {
                    ConnectionType.Serial => new ModbusProtocolSerial(Form.PortName, Form.BaudRate, Form.Parity, Form.DataBits, Form.StopBits),
                    ConnectionType.Network => new ModbusProtocolTcp(Form.IpAddress ?? string.Empty, Form.Port),
                    _ => throw new NotSupportedException()
                };
                ModbusProtocol.Open();
            }
            catch (Exception ex)
            {
                ModbusProtocol = null;
                ErrorMessage = "Can't connect to device.";
                Log.Error(ex, string.Empty);
            }
        }

        public void CloseConnection()
        {
            _cancellationTokenSource?.Cancel();
            ModbusProtocol?.Close();
            ModbusProtocol = null;
        }

        public async void Read()
        {
            if (ModbusProtocol == null || string.IsNullOrEmpty(Form.Directory) || string.IsNullOrEmpty(Form.FileName))
            {
                return;
            }

            if (Form.Count == 0 || 
                Form.Count < 0 || 
                Form.Count > DeviceConstants.MEMORY_SIZE_REGISTERS || 
                Form.Start < 0 || 
                Form.Start + Form.Count * 2 > DeviceConstants.MEMORY_SIZE_BYTES)
            {
                ErrorMessage = "Can't read address start and count is invalid.";
                return;
            }

            ErrorMessage = null;
            Reading = true;
            _cancellationTokenSource = new();
            try
            {
                await Task
                    .Run(() =>
                {
                    var max = 250;
                    var byteCount = Form.Count * 2;
                    var left = byteCount;
                    ushort current = Form.Start;
                    MaxProgress = byteCount;
                    var bytes = ArrayPool<byte>.Shared.Rent(byteCount);
                    while (left > 0 && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var count = int.Min(left, max);
                        var read = ModbusProtocol.ReadRegistersBytes(Form.SlaveId, current, (ushort)(count / 2), _cancellationTokenSource.Token);
                        read.CopyTo(bytes, current - Form.Start);
                        left -= count;
                        current += (ushort)count;
                        CurrentProgress = current - Form.Start;
                    }

                    using var writer = new IntelHexStreamWriter(Path.Combine(Form.Directory, Form.FileName) + ".hex");
                    var byteSpan = bytes.AsSpan();
                    left = byteCount;
                    current = Form.Start;
                    while (left > 0)
                    {
                        var count = int.Min(left, Form.DataRecordLength);
                        writer.WriteDataRecord(current, byteSpan.Slice(current, count).ToArray());
                        left -= count;
                        current += (ushort)count;
                    }
                    writer.Write(":00000001FF");
                    ArrayPool<byte>.Shared.Return(bytes);
                })
                    .ContinueWith(_ =>
                {
                    Reading = false;
                })
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Can't read device memory.";
                Log.Error(ex, string.Empty);
            }
        }

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        public void RefreshPortNames()
        {
            PortNames.Clear();
            var names = SerialPort.GetPortNames();
            foreach (var name in names)
            {
                PortNames.Add(name);
            }
            Form.PortName = PortNames.FirstOrDefault() ?? string.Empty;
        }

        public void ResetStart()
        {
            Form.Start = typeof(FormViewModel).GetProperty(nameof(Form.Start))?.GetCustomAttribute<DefaultValueAttribute>()?.Value as ushort? ?? 0;
        }

        public void ResetCount()
        {
            Form.Count = typeof(FormViewModel).GetProperty(nameof(Form.Count))?.GetCustomAttribute<DefaultValueAttribute>()?.Value as ushort? ?? 0;
        }

        public void ResetDataRecordLength()
        {
            Form.DataRecordLength = typeof(FormViewModel).GetProperty(nameof(Form.DataRecordLength))?.GetCustomAttribute<DefaultValueAttribute>()?.Value as byte? ?? 0;
        }
    }
}
