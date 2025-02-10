using System.Collections.ObjectModel;
using System.IO.Ports;
using FlomtManager.App.Stores;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Repositories;
using FlomtManager.Framework.Helpers;
using ReactiveUI;
using Serilog;

namespace FlomtManager.App.ViewModels
{
    public class DeviceCreateUpdateViewModel : ViewModelBase
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly DeviceStore _deviceStore;

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

        public DeviceCreateUpdateViewModel(IDeviceRepository deviceRepository, DeviceStore deviceStore)
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

        public async void CreateDevice()
        {
            var validationErrors = ValidationHelper.Validate(Form);
            if (validationErrors.Any())
            {
                return;
            }

            var entity = new Device()
            {
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

            try
            {
                _deviceRepository.Add(entity);
                await _deviceRepository.SaveChangesAsync();
                _deviceStore.CreateDevice(await _deviceRepository.GetByIdAsyncNonTracking(entity.Id));

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
                var entity = await _deviceRepository.GetByIdAsync(Form.Id);
                entity.Name = Form.Name;
                entity.Address = Form.Address;
                entity.ConnectionType = Form.ConnectionType;
                entity.SlaveId = Form.SlaveId;
                entity.PortName = Form.PortName;
                entity.BaudRate = Form.BaudRate;
                entity.Parity = Form.Parity;
                entity.DataBits = Form.DataBits;
                entity.StopBits = Form.StopBits;
                entity.IpAddress = Form.IpAddress;
                entity.Port = Form.Port;
                await _deviceRepository.SaveChangesAsync();
                _deviceStore.UpdateDevice(await _deviceRepository.GetByIdAsyncNonTracking(entity.Id));

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
    }
}
