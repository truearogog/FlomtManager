using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDeviceListItemViewModel : IViewModel
{
    Device Device { get; set; }
    bool IsEditable { get; set; }
}
