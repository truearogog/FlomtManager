using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;
using ReactiveUI;

namespace FlomtManager.Application.ViewModels;

internal sealed class DeviceListItemViewModel : ViewModel, IDeviceListItemViewModel
{
    private Device _device;
    public Device Device
    {
        get => _device;
        set => this.RaiseAndSetIfChanged(ref _device, value);
    }

    private bool _isEditable;
    public bool IsEditable
    {
        get => _isEditable;
        set => this.RaiseAndSetIfChanged(ref _isEditable, value);
    }
}
