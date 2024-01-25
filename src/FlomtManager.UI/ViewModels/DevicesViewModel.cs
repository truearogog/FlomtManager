using FlomtManager.Core.Models;
using FlomtManager.Core.Services;
using FlomtManager.UI.Stores;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FlomtManager.UI.ViewModels
{
    public class DevicesViewModel : ViewModelBase
    {
        private readonly IDeviceService _deviceService;
        private readonly DeviceStore _deviceStore;

        public EventHandler DeviceCreateRequested;
        public EventHandler<Device> DeviceUpdateRequested;
        public EventHandler<Device> DeviceViewRequested;

        public ObservableCollection<Device> Devices { get; set; } = [];

        public ICommand CreateDeviceCommand { get; set; }
        public ICommand UpdateDeviceCommand { get; set; }
        public ICommand ViewDeviceCommand { get; set; }

        public DevicesViewModel(IDeviceService deviceService, DeviceStore deviceStore)
        {
            _deviceService = deviceService;
            _deviceStore = deviceStore;

            _deviceStore.DeviceCreated += _DeviceCreated;
            _deviceStore.DeviceUpdated += _DeviceUpdated;
            _deviceStore.DeviceDeleted += _DeviceDeleted;

            CreateDeviceCommand = ReactiveCommand.Create(CreateDevice);
            UpdateDeviceCommand = ReactiveCommand.Create<Device>(UpdateDevice);
            ViewDeviceCommand = ReactiveCommand.Create<Device>(ViewDevice);

            AddDevices().Wait();
        }

        private void CreateDevice()
        {
            DeviceCreateRequested.Invoke(this, EventArgs.Empty);
        }

        private void UpdateDevice(Device device)
        {
            DeviceUpdateRequested.Invoke(this, device);
        }

        private void ViewDevice(Device device)
        {
            DeviceViewRequested.Invoke(this, device);
        }

        private async Task AddDevices()
        {
            Devices.Clear();
            var devices = await _deviceService.GetAll();
            foreach (var device in devices)
            {
                AddDevice(device);
            }
        }

        private void AddDevice(Device device)
        {
            Devices.Add(device);
        }

        private void _DeviceCreated(Device device)
        {
            AddDevice(device);
        }

        private void _DeviceUpdated(Device device)
        {
            if (device != null)
            {
                var index = Devices.IndexOf(Devices.Where(x => x.Id == device.Id).FirstOrDefault());
                if (index != -1)
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
            Devices.Remove(device);
        }
    }
}
