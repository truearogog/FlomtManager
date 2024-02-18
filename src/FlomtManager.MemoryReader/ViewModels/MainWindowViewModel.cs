using Avalonia.Controls.Notifications;
using FlomtManager.Core.Constants;
using FlomtManager.Core.Enums;
using FlomtManager.Modbus;
using HexIO;
using ReactiveUI;
using Serilog;
using System.Buffers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Reflection;

namespace FlomtManager.MemoryReader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public FormViewModel Form { get; set; } = new();

        private string? _successMessage;
        public string? SuccessMessage
        {
            get => _successMessage;
            set => this.RaiseAndSetIfChanged(ref _successMessage, value);
        }

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

        public event EventHandler? DirectoryRequested;
        public event EventHandler<(NotificationType, string)>? NotificationRequested;

        public MainWindowViewModel()
        {
            RefreshPortNames();
        }

        public void RequestDirectory()
        {
            DirectoryRequested?.Invoke(this, EventArgs.Empty);
        }

        private void RequestNotification(NotificationType type, string message)
        {
            NotificationRequested?.Invoke(this, (type, message));
        }

        public async void OpenConnection()
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
                await ModbusProtocol.OpenAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                ModbusProtocol = null;
                ErrorMessage = "Can't connect to device.";
                Log.Error(ex, string.Empty);
            }
        }

        public async void CloseConnection()
        {
            _cancellationTokenSource?.Cancel();
            if (ModbusProtocol != null)
            {
                await ModbusProtocol.CloseAsync(CancellationToken.None);
                ModbusProtocol = null;
            }
        }

        public async void Read()
        {
            if (ModbusProtocol == null || string.IsNullOrEmpty(Form.Directory))
            {
                return;
            }

            if (Form.Count == 0 ||
                Form.Count < 0 ||
                Form.Count > DeviceConstants.MEMORY_SIZE_BYTES ||
                Form.Start < 0 ||
                Form.Start + Form.Count > DeviceConstants.MEMORY_SIZE_BYTES)
            {
                ErrorMessage = "Invalid start address and length in bytes.";
                return;
            }

            MaxProgress = 1;
            ErrorMessage = null;
            SuccessMessage = null;
            Reading = true;
            _cancellationTokenSource = new();
            try
            {
                var fileName = Form.FileName;
                if (string.IsNullOrEmpty(fileName))
                {
                    var deviceNumber = (await ModbusProtocol.ReadRegistersAsync(Form.SlaveId, 100, 1, cancellationToken: _cancellationTokenSource.Token)).First();
                    if (Form.DateTime.Date != DateTime.Now.Date)
                    {
                        Form.DateTime = DateTime.Now;
                        Form.Number = 1;
                    }
                    else
                    {
                        Form.Number++;
                    }
                    fileName = $"flomt_{Form.DateTime:yyMMdd}_{deviceNumber}_S{Form.Start}_L{Form.Count}_No{Form.Number}.hex";
                }

                var bytes = await ModbusProtocol.ReadRegistersBytesAsync(Form.SlaveId, (ushort)Form.Start, (ushort)(Form.Count / 2), (current, total) =>
                {
                    CurrentProgress = current;
                    MaxProgress = total;
                }, _cancellationTokenSource.Token);

                using var writer = new IntelHexStreamWriter(Path.Combine(Form.Directory, fileName));
                int current = 0, left = bytes.Length;
                while (left > 0)
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var count = int.Min(left, Form.DataRecordLength);
                    writer.WriteDataRecord((ushort)(Form.Start + current), bytes.Skip(current).Take(count).ToArray());
                    left -= count;
                    current += (ushort)count;
                }
                writer.Write(":00000001FF");
                RequestNotification(NotificationType.Success, $"Created {fileName}");
                SuccessMessage = $"Read {Form.Count} bytes starting from {Form.Start}.";
            }
            catch (ModbusException ex)
            {
                ErrorMessage = ex.Message;
                Log.Error(ex, string.Empty);
            }
            catch (OperationCanceledException ex)
            {
                ErrorMessage = "Canceled.";
                Log.Error(ex, string.Empty);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Can't read device memory.";
                Log.Error(ex, string.Empty);
            }
            finally
            {
                Reading = false;
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
            Form.Start = typeof(FormViewModel).GetProperty(nameof(Form.Start))?.GetCustomAttribute<DefaultValueAttribute>()?.Value as int? ?? 0;
        }

        public void ResetCount()
        {
            Form.Count = typeof(FormViewModel).GetProperty(nameof(Form.Count))?.GetCustomAttribute<DefaultValueAttribute>()?.Value as int? ?? 0;
        }

        public void ResetDataRecordLength()
        {
            Form.DataRecordLength = typeof(FormViewModel).GetProperty(nameof(Form.DataRecordLength))?.GetCustomAttribute<DefaultValueAttribute>()?.Value as byte? ?? 0;
        }
    }
}
