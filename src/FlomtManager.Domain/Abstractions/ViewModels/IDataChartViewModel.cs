using System.Collections.ObjectModel;
using FlomtManager.Domain.Abstractions.ViewModels.Events;
using FlomtManager.Domain.Models;
using FlomtManager.Domain.Models.Collections;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDataChartViewModel : IViewModel
{
    event EventHandler<Parameter> OnParameterUpdated;
    event EventHandler OnDataUpdated;
    event EventHandler<IntegrationChangedArgs> OnIntegrationChanged;

    IEnumerable<Parameter> VisibleParameters { get; }
    IReadOnlyDictionary<byte, IDataCollection> DataCollections { get; }
    double[] DateTimes { get; }

    Device Device { get; set; }

    ObservableCollection<IParameterViewModel> Parameters { get; set; }

    bool IntegrationSpanActive { get; set; }
    double IntegrationSpanMinDate { get; }
    double IntegrationSpanMaxDate { get; }
    int IntegrationSpanMinIndex { get; }
    int IntegrationSpanMaxIndex { get; }

    double CurrentDisplayDate { get; set; }

    double CurrentDisplaySpanMinDate { get; }
    double CurrentDisplaySpanMaxDate { get; }
    int CurrentDisplaySpanMinIndex { get; }
    int CurrentDisplaySpanMaxIndex { get; }

    Task SetDevice(Device device);
    Task UpdateData();
    void UpdateCurrentDisplaySpanDates(double min, double max);
    void UpdateIntegration(double min, double max);
}
