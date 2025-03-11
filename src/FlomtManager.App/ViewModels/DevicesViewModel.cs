using System.Collections.ObjectModel;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Stores;

namespace FlomtManager.App.ViewModels
{
    public class DevicesViewModel : ViewModel
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IDeviceStore _deviceStore;

        public event EventHandler DeviceCreateRequested;
        public event EventHandler<Device> DeviceUpdateRequested;
        public event EventHandler<Device> DeviceViewRequested;

        public ObservableCollection<Device> Devices { get; set; } = [];

        public DevicesViewModel(IDeviceRepository deviceRepository, IDeviceStore deviceStore)
        {
            _deviceRepository = deviceRepository;
            _deviceStore = deviceStore;

            _deviceStore.DeviceAdded += _deviceStore_DeviceAdded; ;
            _deviceStore.DeviceUpdated += _deviceStore_DeviceUpdated; ;
            _deviceStore.DeviceRemoved += _deviceStore_DeviceRemoved; ;

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

            await foreach (var device in await _deviceRepository.GetAllAsync())
            {
                Devices.Add(device);
            }
        }

        private void _deviceStore_DeviceAdded(object sender, Device e)
        {
            Devices.Add(e);
        }

        private void _deviceStore_DeviceUpdated(object sender, Device e)
        {
            var item = Devices.Where(x => x.Id == e.Id).FirstOrDefault();
            var index = 0;
            if ((index = Devices.IndexOf(item)) != -1)
            {
                Devices[index] = e;
            }
            else
            {
                Devices.Add(e);
            }
        }

        private void _deviceStore_DeviceRemoved(object sender, Device e)
        {
            var device = Devices.FirstOrDefault(x => x.Id == e.Id);
            Devices.Remove(device);
        }
    }
}
