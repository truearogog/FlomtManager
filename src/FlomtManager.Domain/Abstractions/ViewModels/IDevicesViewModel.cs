using System.Collections.ObjectModel;
using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDevicesViewModel : IViewModel
{
    event EventHandler DeviceCreateRequested;
    event EventHandler<Device> DeviceUpdateRequested;
    event EventHandler<Device> DeviceDeleteRequested;
    event EventHandler<Device> DeviceViewRequested;

    ObservableCollection<Device> Devices { get; set; }

    void CreateDevice();
    void UpdateDevice(Device device);
    void DeleteDevice(Device device);
    void ViewDevice(Device device);
    Task LoadDevices();
}
