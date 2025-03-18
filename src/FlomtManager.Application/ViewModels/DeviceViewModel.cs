using System.Collections.ObjectModel;
using FlomtManager.Domain.Abstractions.DeviceConnection;
using FlomtManager.Domain.Abstractions.Providers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Abstractions.ViewModels.Events;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;
using FlomtManager.Modbus;
using ReactiveUI;

namespace FlomtManager.Application.ViewModels;

internal sealed class DeviceViewModel : ViewModel, IDeviceViewModel
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceConnectionFactory _deviceConnectionFactory;
    private readonly IFileDataImporterFactory _fileDataImporterFactory;
    private readonly IParameterViewModelFactory _parameterViewModelFactory;
    private readonly IParameterRepository _parameterRepository;

    public event EventHandler CloseRequested;
    public event EventHandler<Device> DeviceUpdateRequested;
    public event EventHandler ReadFromFileRequested;

    private Device _device = default;
    public Device Device
    {
        get => _device;
        set
        {
            this.RaiseAndSetIfChanged(ref _device, value);
        }
    }

    private ArchiveDisplayMode _archiveDisplayMode = ArchiveDisplayMode.Chart;
    public ArchiveDisplayMode ArchiveDisplayMode
    {
        get => _archiveDisplayMode;
        set => this.RaiseAndSetIfChanged(ref _archiveDisplayMode, value);
    }

    public ObservableCollection<IParameterViewModel> CurrentParameters { get; set; } = [];
    public ObservableCollection<IParameterViewModel> IntegralParameters { get; set; } = [];

    #region Device Connection

    private IDeviceConnection _deviceConnection;

    private DateTime? _lastTimeDataRead = null;
    public DateTime? LastTimeDataRead
    {
        get => _lastTimeDataRead;
        set => this.RaiseAndSetIfChanged(ref _lastTimeDataRead, value);
    }

    private DateTime? _lastTimeArchiveRead = null;
    public DateTime? LastTimeArchiveRead
    {
        get => _lastTimeArchiveRead;
        set => this.RaiseAndSetIfChanged(ref _lastTimeArchiveRead, value);
    }

    private int _archiveReadingProgress = 0;
    public int ArchiveReadingProgress
    {
        get => _archiveReadingProgress;
        set => this.RaiseAndSetIfChanged(ref _archiveReadingProgress, value);
    }

    private ArchiveReadingState _archiveReadingState = ArchiveReadingState.None;
    public ArchiveReadingState ArchiveReadingState
    {
        get => _archiveReadingState;
        set => this.RaiseAndSetIfChanged(ref _archiveReadingState, value);
    }

    private ConnectionState _connectionState = ConnectionState.Disconnected;
    public ConnectionState ConnectionState
    {
        get => _connectionState;
        set => this.RaiseAndSetIfChanged(ref _connectionState, value);
    }

    #endregion

    public IDataChartViewModel DataChart { get; set; }
    public IDataTableViewModel DataTable { get; set; }
    public IDataIntegrationViewModel DataIntegration { get; set; }

    public DeviceViewModel(
            IDateTimeProvider dateTimeProvider,
            IDeviceStore deviceStore,
            IDeviceConnectionFactory deviceConnectionFactory,
            IFileDataImporterFactory fileDataImporterFactory,
            IParameterViewModelFactory parameterViewModelFactory,
            IParameterRepository parameterRepository,
            IDataChartViewModel dataChart,
            IDataTableViewModel dataTable,
            IDataIntegrationViewModel dataIntegration)
    {
        _dateTimeProvider = dateTimeProvider;
        _deviceStore = deviceStore;
        _deviceConnectionFactory = deviceConnectionFactory;
        _fileDataImporterFactory = fileDataImporterFactory;
        _parameterViewModelFactory = parameterViewModelFactory;
        _parameterRepository = parameterRepository;

        _deviceStore.DeviceUpdated += _deviceStore_DeviceUpdated;
        _deviceStore.DeviceRemoved += _deviceStore_DeviceRemoved;

        DataChart = dataChart;
        DataChart.OnIntegrationChanged += DataChart_OnIntegrationChanged;

        DataTable = dataTable;

        DataIntegration = dataIntegration;
    }

    public async Task SetDevice(Device device)
    {
        Device = device;
        await DataChart.SetDevice(device);
        await DataTable.SetDevice(device);
        await DataIntegration.SetDevice(device);
        await UpdateParameters();
    }

    private async Task UpdateParameters()
    {
        if (Device == default)
        {
            return;
        }

        CurrentParameters.Clear();
        var currentParameters = await _parameterRepository.GetCurrentParametersByDeviceId(Device.Id);
        foreach (var currentParameter in currentParameters)
        {
            var parameterViewModel = _parameterViewModelFactory.Create(currentParameter);
            CurrentParameters.Add(parameterViewModel);
        }

        IntegralParameters.Clear();
        var integralParameters = await _parameterRepository.GetIntegralParametersByDeviceId(Device.Id);
        foreach (var integralParameter in integralParameters)
        {
            var parameterViewModel = _parameterViewModelFactory.Create(integralParameter);
            IntegralParameters.Add(parameterViewModel);
        }
    }

    public void RequestDeviceUpdate(Device device)
    {
        DeviceUpdateRequested?.Invoke(this, device);
    }

    public void SetDataDisplayMode(ArchiveDisplayMode mode)
    {
        ArchiveDisplayMode = mode;
    }

    public async Task TryConnect()
    {
        try
        {
            _deviceConnection = _deviceConnectionFactory.Create(Device);

            _deviceConnection.OnConnectionError += async (sender, e) =>
            {
                // todo: log error
                await TryDisconnect();
            };

            _deviceConnection.OnConnectionStateChanged += (sender, e) => ConnectionState = e.State;

            _deviceConnection.OnConnectionDataChanged += (sender, e) =>
            {
                foreach (var currentParameter in CurrentParameters)
                {
                    ParameterValue value = e.CurrentParameterValues.TryGetValue(currentParameter.Parameter.Number, out var _value) ? _value : null;
                    if (value != null)
                    {
                        currentParameter.Value = value.Value;
                        currentParameter.Error = value.Error;
                    }
                }
                foreach (var integralParameter in IntegralParameters)
                {
                    ParameterValue value = e.IntegralParameterValues.TryGetValue(integralParameter.Parameter.Number, out var _value) ? _value : null;
                    if (value != null)
                    {
                        integralParameter.Value = value.Value;
                        integralParameter.Error = value.Error;
                    }
                }

                LastTimeDataRead = _dateTimeProvider.Now;
            };

            _deviceConnection.OnConnectionArchiveReadingStateChanged += async (sender, e) =>
            {
                ArchiveReadingState = e.State;
                if (e.State == ArchiveReadingState.Complete)
                {
                    LastTimeArchiveRead = _dateTimeProvider.Now;
                    if (e.LinesRead > 0)
                    {
                        await DataChart.UpdateData();
                        await DataTable.UpdateData();
                    }
                }
            };

            _deviceConnection.OnConnectionArchiveReadingProgressChangedArgs += (sender, e) =>
            {
                ArchiveReadingProgress = (int)((double)e.Progress / e.MaxProgress * 100);
            };

            await _deviceConnection.Connect();
        }
        catch (ModbusException)
        {
            // todo: log error
            await (_deviceConnection?.DisposeAsync() ?? ValueTask.CompletedTask);
            _deviceConnection = null;
        }
    }

    public async Task TryDisconnect()
    {
        await (_deviceConnection?.Disconnect() ?? Task.CompletedTask);
        await (_deviceConnection?.DisposeAsync() ?? ValueTask.CompletedTask);
        _deviceConnection = null;
    }

    public void RequestReadFile()
    {
        ReadFromFileRequested?.Invoke(this, EventArgs.Empty);
    }

    public async Task ReadFile(Stream stream)
    {
        try
        {
            var fileDataImporter = _fileDataImporterFactory.Create(Device);

            fileDataImporter.OnConnectionError += (sender, e) =>
            {
                // todo: log error
            };

            fileDataImporter.OnConnectionStateChanged += (sender, e) => ConnectionState = e.State;

            fileDataImporter.OnConnectionDataChanged += (sender, e) =>
            {
                foreach (var currentParameter in CurrentParameters)
                {
                    ParameterValue value = e.CurrentParameterValues.TryGetValue(currentParameter.Parameter.Number, out var _value) ? _value : null;
                    if (value != null)
                    {
                        currentParameter.Value = value.Value;
                        currentParameter.Error = value.Error;
                    }
                }
                foreach (var integralParameter in IntegralParameters)
                {
                    ParameterValue value = e.IntegralParameterValues.TryGetValue(integralParameter.Parameter.Number, out var _value) ? _value : null;
                    if (value != null)
                    {
                        integralParameter.Value = value.Value;
                        integralParameter.Error = value.Error;
                    }
                }

                LastTimeDataRead = _dateTimeProvider.Now;
            };

            fileDataImporter.OnConnectionArchiveReadingStateChanged += async (sender, e) =>
            {
                ArchiveReadingState = e.State;
                if (e.State == ArchiveReadingState.Complete)
                {
                    LastTimeArchiveRead = _dateTimeProvider.Now;
                    if (e.LinesRead > 0)
                    {
                        await DataChart.UpdateData();
                        await DataTable.UpdateData();
                    }
                }
            };

            fileDataImporter.OnConnectionArchiveReadingProgressChangedArgs += (sender, e) =>
            {
                ArchiveReadingProgress = (int)((double)e.Progress / e.MaxProgress * 100);
            };

            await fileDataImporter.Import(stream);
        }
        catch (Exception)
        {
            // todo: log error
        }
    }

    private async void _deviceStore_DeviceUpdated(object sender, Device device)
    {
        if (Device.Id == device.Id)
        {
            await SetDevice(device);
        }
    }

    private void _deviceStore_DeviceRemoved(object sender, Device device)
    {
        if (Device.Id == device.Id)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void DataChart_OnIntegrationChanged(object sender, IntegrationChangedArgs e)
    {
        DataIntegration.UpdateValues(e);
    }
}
