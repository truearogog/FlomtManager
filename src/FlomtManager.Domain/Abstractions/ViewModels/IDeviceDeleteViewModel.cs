using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDeviceDeleteViewModel : IViewModel
{
    event EventHandler CloseRequested;

    Device Device { get; }
    string ErrorMessage { get; }
    bool CanDelete { get; }
    int CanDeleteCount { get; }

    void SetDevice(Device device);
    void RequestClose();
    void DeleteDevice();
}
