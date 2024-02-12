using FlomtManager.App.Stores;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FlomtManager.App.ViewModels
{
    public class DevicesViewModel : ViewModelBase
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly DeviceStore _deviceStore;

        public event EventHandler? DeviceCreateRequested;
        public event EventHandler<Device>? DeviceUpdateRequested;
        public event EventHandler<Device>? DeviceViewRequested;

        public ObservableCollection<Device> Devices { get; set; } = [];

        public DevicesViewModel(IDeviceRepository deviceRepository, DeviceStore deviceStore)
        {
            _deviceRepository = deviceRepository;
            _deviceStore = deviceStore;

            _deviceStore.DeviceCreated += _DeviceCreated;
            _deviceStore.DeviceUpdated += _DeviceUpdated;
            _deviceStore.DeviceDeleted += _DeviceDeleted;

            AddDevices();
        }

        public void CreateDevice()
        {
            DeviceCreateRequested?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateDevice(Device device)
        {
            DeviceUpdateRequested?.Invoke(this, device);
        }

        public void ViewDevice(Device device)
        {
            DeviceViewRequested?.Invoke(this, device);
        }

        private async void AddDevices()
        {
            Devices.Clear();
            await foreach (var device in _deviceRepository.GetAllAsync())
            {
                Devices.Add(device);
            }
        }

        private void _DeviceCreated(Device device)
        {
            Devices.Add(device);
        }

        private void _DeviceUpdated(Device device)
        {
            if (device != null)
            {
                var item = Devices.Where(x => x.Id == device.Id).FirstOrDefault();
                var index = 0;
                if (item != null && (index = Devices.IndexOf(item)) != -1)
                {
                    Devices[index] = device;
                }
                else
                {
                    Devices.Add(device);
                }
            }
        }

        private void _DeviceDeleted(int id)
        {
            var device = Devices.FirstOrDefault(x => x.Id == id);
            Devices.Remove(device!);
        }
    }
}
