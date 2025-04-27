using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModelFactories;

public interface IDeviceFormViewModelFactory
{
    Task<IDeviceFormViewModel> Create();
    IDeviceFormViewModel Create(Device device);
}
