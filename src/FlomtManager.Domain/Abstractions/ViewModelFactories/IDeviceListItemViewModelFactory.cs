using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModelFactories;

public interface IDeviceListItemViewModelFactory
{
    IDeviceListItemViewModel Create(Device device);
}
