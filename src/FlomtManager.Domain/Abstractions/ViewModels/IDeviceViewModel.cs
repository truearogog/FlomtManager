using System.Collections.ObjectModel;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDeviceViewModel : IViewModel
{
    event EventHandler CloseRequested;
    event EventHandler<Device> DeviceUpdateRequested;
    event EventHandler ReadFromFileRequested;

    Device Device { get; set; }
    bool IsEditable { get; set; }

    DeviceViewMode DeviceViewMode { get; set; }

    ObservableCollection<IParameterViewModel> CurrentParameters { get; set; }
    ObservableCollection<IParameterViewModel> IntegralParameters { get; set; }

    #region Device Connection

    DateTime? LastTimeDataRead { get; set; }
    DateTime? LastTimeArchiveRead { get; set; }
    int ArchiveReadingProgress { get; set; }
    ArchiveReadingState ArchiveReadingState { get; set; }
    ConnectionState ConnectionState { get; set; }

    #endregion

    IDataChartViewModel DataChart { get; set; }
    IDataTableViewModel DataTable { get; set; }
    IDataIntegrationViewModel DataIntegration { get; set; }

    Task SetDevice(Device device);
    void RequestDeviceUpdate(Device device);
    void SetDataDisplayMode(DeviceViewMode mode);
    Task TryConnect();
    Task TryDisconnect();
    void RequestReadFile();
    Task ReadFile(Stream stream);
    Task SaveRealtimeParameters();
}
