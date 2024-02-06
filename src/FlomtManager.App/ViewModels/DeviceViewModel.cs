using Avalonia.Controls.Notifications;
using FlomtManager.App.Stores;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Services;
using FlomtManager.Modbus;
using ReactiveUI;
using Serilog;
using System;
using System.Threading;

namespace FlomtManager.App.ViewModels
{
    public class DeviceViewModel : ViewModelBase
    {
        private readonly DeviceStore _deviceStore;
        private readonly IModbusService _modbusService;

        public EventHandler? CloseRequested;
        public EventHandler<Device>? DeviceUpdateRequested;

        private Device? _device;
        public Device? Device
        {
            get => _device;
            set => this.RaiseAndSetIfChanged(ref _device, value);
        }

        private IModbusProtocol? _modbusProtocol;
        public IModbusProtocol? ModbusProtocol
        {
            get => _modbusProtocol;
            set => this.RaiseAndSetIfChanged(ref _modbusProtocol, value);
        }

        public EventHandler<(NotificationType, string)>? NotificationRequested { get; set; }

        public DeviceViewModel(DeviceStore deviceStore, IModbusService modbusService)
        {
            _deviceStore = deviceStore;
            _modbusService = modbusService;

            _deviceStore.DeviceUpdated += _DeviceUpdated;
            _deviceStore.DeviceDeleted += _DeviceDeleted;
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

        public void TryConnect()
        {
            if (Device != null)
            {
                try
                {
                    ModbusProtocol = Device.ConnectionType switch
                    {
                        ConnectionType.Serial => new ModbusProtocolSerial(Device.PortName, Device.BaudRate, Device.Parity, Device.DataBits, Device.StopBits),
                        ConnectionType.Network => new ModbusProtocolTcp(Device.IpAddress, Device.Port),
                        _ => throw new NotSupportedException("Not supported device connection type.")
                    };
                    ModbusProtocol.Open();
                    ModbusProtocol.Close();
                    NotificationRequested?.Invoke(this, (NotificationType.Success, "Connected."));
                }
                catch (Exception ex)
                {
                    ModbusProtocol = null;
                    NotificationRequested?.Invoke(this, (NotificationType.Error, "Can't connect to device."));
                    Log.Error(ex, string.Empty);
                    return;
                }

                if (Device.DeviceDefinitionId == 0)
                {
                    ModbusProtocol.Open();

                    var deviceDefinitions = _modbusService.ReadDeviceDefinitions(ModbusProtocol, Device.SlaveId, CancellationToken.None);
                    var parameters = _modbusService.ReadParameterDefinitions(ModbusProtocol, Device.SlaveId, deviceDefinitions, CancellationToken.None);
                    var currentParameters = _modbusService.ReadCurrentParameters(ModbusProtocol, Device.SlaveId, deviceDefinitions, CancellationToken.None);

                    ModbusProtocol.Close();
                }
            }
        }
    }
}
