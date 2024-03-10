#nullable disable

using AutoMapper;
using FlomtManager.App.Stores;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Framework.Helpers;
using ReactiveUI;
using Serilog;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace FlomtManager.App.ViewModels
{
    public class DeviceCreateUpdateViewModel : ViewModelBase
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly DeviceStore _deviceStore;
        private readonly IMapper _mapper;

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

        public DeviceCreateUpdateViewModel(IDeviceRepository deviceRepository, DeviceStore deviceStore, IMapper mapper)
        {
            _deviceRepository = deviceRepository;
            _deviceStore = deviceStore;
            _mapper = mapper;

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

            var model = _mapper.Map<Device>(Form);
            try
            {
                await _deviceStore.CreateDevice(_deviceRepository, model);
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

            var model = _mapper.Map<Device>(Form);
            try
            {
                await _deviceStore.UpdateDevice(_deviceRepository, model);
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
            Form = _mapper.Map<DeviceFormViewModel>(device);
        }
    }
}
