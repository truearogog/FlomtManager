using System.Collections.ObjectModel;
using FlomtManager.Domain.Abstractions.ViewModels.Events;
using FlomtManager.Domain.Models;
using FlomtManager.Domain.Models.Collections;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDataChartViewModel : IViewModel
{
    event EventHandler<Parameter> OnParameterUpdated;
    event EventHandler OnDataUpdated;
    event EventHandler<byte> OnParameterToggled;
    event EventHandler<IntegrationChangedArgs> OnIntegrationChanged;

    IEnumerable<Parameter> VisibleParameters { get; }
    IReadOnlyDictionary<byte, IDataCollection> DataCollections { get; }
    double[] DateTimes { get; }

    Device Device { get; set; }

    ObservableCollection<IParameterViewModel> Parameters { get; set; }

    double IntegrationSpanMinDate { get; set; }
    double IntegrationSpanMaxDate { get; set; }
    double CurrentDisplayDate { get; set; }

    Task SetDevice(Device device);
    Task UpdateData();
    void ToggleParameter(byte parameterNumber);
}
