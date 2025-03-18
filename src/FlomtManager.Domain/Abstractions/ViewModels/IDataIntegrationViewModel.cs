using System.Collections.ObjectModel;
using FlomtManager.Domain.Abstractions.ViewModels.Events;
using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDataIntegrationViewModel
{
    Device Device { get; set; }
    DateTime? IntegrationStart { get; set; }
    DateTime? IntegrationEnd { get; set; }
    ObservableCollection<IParameterViewModel> Parameters { get; set; }

    Task SetDevice(Device device);
    void UpdateValues(IntegrationChangedArgs args);
}
