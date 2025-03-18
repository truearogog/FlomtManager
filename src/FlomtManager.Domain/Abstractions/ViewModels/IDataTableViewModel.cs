using System.Collections.ObjectModel;
using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDataTableViewModel : IViewModel
{
    event EventHandler OnDataUpdated;

    ObservableCollection<ValueCollection> Data { get; }
    IReadOnlyDictionary<byte, byte> ParameterPositions { get; }

    Device Device { get; set; }
    List<Parameter> Parameters { get; }

    Task SetDevice(Device device);
    Task UpdateData();

    public readonly struct ValueCollection(int size)
    {
        public object[] Values { get; } = new object[size];
    }
}
