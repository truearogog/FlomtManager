using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDeviceCreateUpdateViewModel : IViewModel
{
    event EventHandler CloseRequested;

    IDeviceFormViewModel Form { get; set; }
    string ErrorMessage { get; set; }

    ObservableCollection<ConnectionType> ConnectionTypes { get; set; }
    ObservableCollection<string> PortNames { get; set; }
    ObservableCollection<int> BaudRates { get; set; }
    ObservableCollection<Parity> Parities { get; set; }
    ObservableCollection<int> DataBits { get; set; }
    ObservableCollection<StopBits> StopBits { get; set; }

    void SetDevice(Device device);

    void RefreshPortNames();
    void RequestClose();
    Task CreateDevice();
    void UpdateDevice();
}
