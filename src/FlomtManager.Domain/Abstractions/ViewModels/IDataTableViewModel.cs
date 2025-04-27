using System.Collections.ObjectModel;
using FlomtManager.Domain.Models;
using FlomtManager.Domain.Models.Collections;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDataTableViewModel : IViewModel
{
    event EventHandler OnDataUpdated;
    event EventHandler<(int Min, int Max)> OnCurrentDisplaySpanChanged;

    ObservableCollection<StringValueCollection> Data { get; }
    IReadOnlyDictionary<byte, byte> ParameterPositions { get; }

    Device Device { get; }
    IParameterViewModel DateTimeParameter { get; }
    ObservableCollection<IParameterViewModel> Parameters { get; }

    Task SetDevice(Device device);
    Task UpdateData();
    void UpdateCurrentDisplaySpan(int min, int max);
}
