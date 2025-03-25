using FlomtManager.Application.ViewModels;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;

namespace FlomtManager.Application.ViewModelFactories;

internal sealed class DeviceListItemViewModelFactory(IDeviceIsEditableStore deviceIsEditableStore) : IDeviceListItemViewModelFactory
{
    public IDeviceListItemViewModel Create(Device device)
    {
        return new DeviceListItemViewModel
        {
            Device = device,
            IsEditable = !deviceIsEditableStore.TryGetDeviceIsEditable(device.Id, out var isEditable) || isEditable
        };
    }
}
