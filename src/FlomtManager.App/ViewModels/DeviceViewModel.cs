using Avalonia.Controls.Notifications;
using FlomtManager.App.Models;
using FlomtManager.App.Stores;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;
using FlomtManager.Modbus;
using ReactiveUI;
using Serilog;
using System.Collections.ObjectModel;

namespace FlomtManager.App.ViewModels
{
    public class DeviceViewModel : ViewModelBase
    {
        private readonly DeviceStore _deviceStore;
        private readonly DeviceConnectionStore _deviceConnectionStore;
        private readonly IModbusService _modbusService;
        private readonly IDeviceDefinitionRepository _deviceDefinitionRepository;
        private readonly IParameterRepository _parameterRepository;

        private CancellationTokenSource? _modbusCancellationTokenSource;

        public event EventHandler? CloseRequested;
        public event EventHandler<Device>? DeviceUpdateRequested;
        public event EventHandler<(NotificationType, string)>? NotificationRequested;

        private Device? _device;
        public Device? Device
        {
            get => _device;
            set 
            {
                this.RaiseAndSetIfChanged(ref _device, value);
                if (Device != null)
                {
                    ConnectionState = _deviceConnectionStore.GetConnectionState(Device.Id);
                    AddParameters();
                }
            }
        }

        private ConnectionState _connectionState;
        public ConnectionState ConnectionState
        {
            get => _connectionState;
            set => this.RaiseAndSetIfChanged(ref _connectionState, value);
        }

        public ObservableCollection<ParameterViewModel> CurrentParameters { get; set; } = [];
        public ObservableCollection<ParameterViewModel> IntegralParameters { get; set; } = [];

        public DeviceViewModel(DeviceStore deviceStore, DeviceConnectionStore deviceConnectionStore, IModbusService modbusService, 
            IDeviceDefinitionRepository deviceDefinitionRepository, IParameterRepository parameterRepository)
        {
            _deviceStore = deviceStore;
            _deviceConnectionStore = deviceConnectionStore;
            _modbusService = modbusService;
            _deviceDefinitionRepository = deviceDefinitionRepository;
            _parameterRepository = parameterRepository;

            _deviceStore.DeviceUpdated += _DeviceUpdated;
            _deviceStore.DeviceDeleted += _DeviceDeleted;

            _deviceConnectionStore.OnDeviceConnectionState += _OnDeviceConnectionState;
            _deviceConnectionStore.OnDeviceConnectionError += _OnDeviceConnectionError;
        }

        private void _OnDeviceConnectionState(object? sender, DeviceConnectionStateEventArgs e)
        {
            if (e.DeviceId == Device?.Id)
            {
                ConnectionState = e.ConnectionState;
            }
        }

        private void _OnDeviceConnectionError(object? sender, DeviceConnectionErrorEventArgs e)
        {
            if (e.DeviceId == Device?.Id)
            {
                NotificationRequested?.Invoke(this, (NotificationType.Error, "Connection error."));
            }
        }

        private async void AddParameters()
        {
            if (Device == null)
            {
                return;
            }

            var deviceDefinition = Device.DeviceDefinition ?? _deviceDefinitionRepository.GetAllQueryable(x => x.DeviceId == Device.Id).FirstOrDefault();
            if (deviceDefinition == null)
            {
                return;
            }

            CurrentParameters.Clear();
        }

        public void UpdateDevice(Device device)
        {
            DeviceUpdateRequested?.Invoke(this, device);
        }

        private void _DeviceUpdated(Device device)
        {
            if (Device?.Id == device.Id)
            {
                Device = device;
            }
        }

        private void _DeviceDeleted(int id)
        {
            if (Device?.Id == id)
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        public async void TryConnect()
        {
            if (Device == null)
            {
                return;
            }

            try
            {
                await _deviceConnectionStore.TryConnect(Device);
                NotificationRequested?.Invoke(this, (NotificationType.Success, "Connected."));
                AddParameters();
            }
            catch (ModbusException ex)
            {
                NotificationRequested?.Invoke(this, (NotificationType.Error, ex.Message));
                Log.Error(ex, string.Empty);
            }
            catch (Exception ex)
            {
                NotificationRequested?.Invoke(this, (NotificationType.Error, "Can't connect to device."));
                Log.Error(ex, string.Empty);
            }
        }
    }
}
