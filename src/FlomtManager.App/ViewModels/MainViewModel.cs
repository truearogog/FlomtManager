using FlomtManager.Domain.Abstractions.ViewModels;
using ReactiveUI;

namespace FlomtManager.App.ViewModels;

internal sealed class MainViewModel(IDevicesViewModel devicesViewModel) : ReactiveObject
{
    public IDevicesViewModel DevicesViewModel => devicesViewModel;
}
