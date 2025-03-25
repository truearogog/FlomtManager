using System.Collections.ObjectModel;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.Stores.Events;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;

namespace FlomtManager.Application.ViewModels;

internal sealed class DevicesViewModel : ViewModel, IDevicesViewModel
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceIsEditableStore _deviceIsEditableStore;
    private readonly IDeviceListItemViewModelFactory _deviceListItemViewModelFactory;

    public event EventHandler DeviceCreateRequested;
    public event EventHandler<Device> DeviceUpdateRequested;
    public event EventHandler<Device> DeviceDeleteRequested;
    public event EventHandler<Device> DeviceViewRequested;

    public ObservableCollection<IDeviceListItemViewModel> Devices { get; } = [];

    public DevicesViewModel(
        IDeviceRepository deviceRepository, 
        IDeviceStore deviceStore,
        IDeviceIsEditableStore deviceIsEditableStore,
        IDeviceListItemViewModelFactory deviceListItemViewModelFactory)
    {
        _deviceRepository = deviceRepository;
        _deviceStore = deviceStore;
        _deviceIsEditableStore = deviceIsEditableStore;
        _deviceListItemViewModelFactory = deviceListItemViewModelFactory;

        _deviceStore.Added += _deviceStore_DeviceAdded;
        _deviceStore.Updated += _deviceStore_DeviceUpdated;
        _deviceStore.Removed += _deviceStore_DeviceRemoved;

        _deviceIsEditableStore.DeviceIsEditableUpdated += _deviceIsEditableStore_DeviceIsEditableUpdated;
    }

    public void CreateDevice()
    {
        DeviceCreateRequested?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateDevice(Device device)
    {
        DeviceUpdateRequested?.Invoke(this, device);
    }

    public void DeleteDevice(Device device)
    {
        DeviceDeleteRequested?.Invoke(this, device);
    }

    public void ViewDevice(Device device)
    {
        DeviceViewRequested?.Invoke(this, device);
    }

    public async Task LoadDevices()
    {
        Devices.Clear();

        await foreach (var device in await _deviceRepository.GetAllAsync())
        {
            Devices.Add(_deviceListItemViewModelFactory.Create(device));
        }
    }

    private void _deviceStore_DeviceAdded(object sender, Device e)
    {
        Devices.Add(_deviceListItemViewModelFactory.Create(e));
    }

    private void _deviceStore_DeviceUpdated(object sender, Device e)
    {
        var item = Devices.Where(x => x.Device.Id == e.Id).FirstOrDefault();
        var index = 0;
        if ((index = Devices.IndexOf(item)) != -1)
        {
            Devices[index].Device = e;
        }
        else
        {
            Devices.Add(_deviceListItemViewModelFactory.Create(e));
        }
    }

    private void _deviceStore_DeviceRemoved(object sender, Device e)
    {
        var viewModel = Devices.FirstOrDefault(x => x.Device.Id == e.Id);
        Devices.Remove(viewModel);
    }

    private void _deviceIsEditableStore_DeviceIsEditableUpdated(object sender, DeviceIsEditableArgs e)
    {
        var viewModel = Devices.FirstOrDefault(x => x.Device.Id == e.DeviceId);
        if (viewModel != null)
        {
            viewModel.IsEditable = e.IsEditable;
        }
    }
}
