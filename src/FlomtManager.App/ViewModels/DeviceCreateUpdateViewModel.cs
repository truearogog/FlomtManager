using System.Collections.ObjectModel;
using System.IO.Ports;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Stores;
using FlomtManager.Framework.Helpers;
using ReactiveUI;
using Serilog;

namespace FlomtManager.App.ViewModels
{
    public class DeviceCreateUpdateViewModel : ViewModel
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IDeviceStore _deviceStore;

        public DeviceFormViewModel Form { get; set; } = new();

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage; 
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public ObservableCollection<ConnectionType> ConnectionTypes { get; set; } = new(Enum.GetValues<ConnectionType>());
        public ObservableCollection<string> PortNames { get; set; } = [];
        public ObservableCollection<int> BaudRates { get; set; } = [110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000];
        public ObservableCollection<Parity> Parities { get; set; } = new(Enum.GetValues<Parity>());
        public ObservableCollection<int> DataBits { get; set; } = [5, 6, 7, 8];
        public ObservableCollection<StopBits> StopBits { get; set; } = new(Enum.GetValues<StopBits>());

        public EventHandler CloseRequested;

        public DeviceCreateUpdateViewModel(IDeviceRepository deviceRepository, IDeviceStore deviceStore)
        {
            _deviceRepository = deviceRepository;
            _deviceStore = deviceStore;

            RefreshPortNames();
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

        public void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public async Task CreateDevice()
        {
            var validationErrors = ValidationHelper.Validate(Form);
            if (validationErrors.Any())
            {
                return;
            }

            try
            {
                var device = GetDevice();
                var id = await _deviceRepository.Create(device);
                device = device with { Id = id };
                _deviceStore.Add(device);

                Close();
            }
            catch (Exception ex)
            {
                Log.Error(string.Empty, ex);
                ErrorMessage = "Couldn't add new device.";
            }
        }

        public async void UpdateDevice()
        {
            var validationErrors = ValidationHelper.Validate(Form);
            if (validationErrors.Any())
            {
                return;
            }

            try
            {
                var device = GetDevice();
                await _deviceRepository.Update(device);
                _deviceStore.Update(device);

                Close();
            }
            catch (Exception ex)
            {
                Log.Error(string.Empty, ex);
                ErrorMessage = "Couldn't edit device.";
            }
        }

        public void SetDevice(Device device)
        {
            Form = new()
            {
                Id = device.Id,
                Name = device.Name,
                Address = device.Address,
                ConnectionType = device.ConnectionType,
                SlaveId = device.SlaveId,
                PortName = device.PortName,
                BaudRate = device.BaudRate,
                Parity = device.Parity,
                DataBits = device.DataBits,
                StopBits = device.StopBits,
                IpAddress = device.IpAddress,
                Port = device.Port,
            };
        }

        private Device GetDevice()
        {
            return new Device
            {
                Id = Form.Id,
                Name = Form.Name,
                Address = Form.Address,
                ConnectionType = Form.ConnectionType,
                SlaveId = Form.SlaveId,
                PortName = Form.PortName,
                BaudRate = Form.BaudRate,
                Parity = Form.Parity,
                DataBits = Form.DataBits,
                StopBits = Form.StopBits,
                IpAddress = Form.IpAddress,
                Port = Form.Port,
            };
        }
    }
}
