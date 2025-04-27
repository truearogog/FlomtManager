using System.Collections.ObjectModel;
using System.Text.Json;
using FlomtManager.Domain.Abstractions.DeviceConnection;
using FlomtManager.Domain.Abstractions.Providers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Abstractions.ViewModels.Events;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;
using ReactiveUI;
using Serilog;

namespace FlomtManager.Application.ViewModels;

internal sealed class DeviceViewModel : ViewModel, IDeviceViewModel
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceIsEditableStore _deviceIsEditableStore;
    private readonly IDeviceConnectionFactory _deviceConnectionFactory;
    private readonly IFileDataImporterFactory _fileDataImporterFactory;
    private readonly IParameterViewModelFactory _parameterViewModelFactory;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IParameterRepository _parameterRepository;
    private readonly ILogger _logger = Log.ForContext<DeviceViewModel>();

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

    private bool _isEditable = true;
    public bool IsEditable
    {
        get => _isEditable;
        set => this.RaiseAndSetIfChanged(ref _isEditable, value);
    }

    private DeviceViewMode _archiveDisplayMode = DeviceViewMode.Chart;
    public DeviceViewMode DeviceViewMode
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
            IDeviceIsEditableStore deviceIsEditableStore,
            IDeviceConnectionFactory deviceConnectionFactory,
            IFileDataImporterFactory fileDataImporterFactory,
            IParameterViewModelFactory parameterViewModelFactory,
            IDeviceRepository deviceRepository,
            IParameterRepository parameterRepository,
            IDataChartViewModel dataChart,
            IDataTableViewModel dataTable,
            IDataIntegrationViewModel dataIntegration)
    {
        _dateTimeProvider = dateTimeProvider;
        _deviceStore = deviceStore;
        _deviceIsEditableStore = deviceIsEditableStore;
        _deviceConnectionFactory = deviceConnectionFactory;
        _fileDataImporterFactory = fileDataImporterFactory;
        _parameterViewModelFactory = parameterViewModelFactory;
        _deviceRepository = deviceRepository;
        _parameterRepository = parameterRepository;

        _deviceStore.Updated += _deviceStore_Updated;
        _deviceStore.Removed += _deviceStore_Removed;

        _deviceIsEditableStore.DeviceIsEditableUpdated += _deviceIsEditableStore_DeviceIsEditableUpdated;

        DataChart = dataChart;
        DataChart.OnIntegrationChanged += DataChart_OnIntegrationChanged;

        DataTable = dataTable;

        DataIntegration = dataIntegration;
    }

    public async Task Activate(Device device)
    {
        Device = device;
        IsEditable = !_deviceIsEditableStore.TryGetDeviceIsEditable(Device.Id, out var isEditable) || isEditable;
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

        var realTimeValuesJson = await _deviceRepository.GetRealTimeValues(Device.Id);
        var integralValues = string.IsNullOrEmpty(realTimeValuesJson) ? [] : JsonSerializer.Deserialize<Dictionary<byte, string>>(realTimeValuesJson.Split('|')[0]);
        var currentValues = string.IsNullOrEmpty(realTimeValuesJson) ? [] : JsonSerializer.Deserialize<Dictionary<byte, string>>(realTimeValuesJson.Split('|')[1]);

        CurrentParameters.Clear();
        var currentParameters = await _parameterRepository.GetCurrentParametersByDeviceId(Device.Id);
        foreach (var currentParameter in currentParameters)
        {
            var parameterViewModel = _parameterViewModelFactory.Create(currentParameter);
            if (currentValues.TryGetValue(currentParameter.Number, out var currentValue))
            {
                parameterViewModel.Value = currentValue;
            }
            CurrentParameters.Add(parameterViewModel);
        }

        IntegralParameters.Clear();
        var integralParameters = await _parameterRepository.GetIntegralParametersByDeviceId(Device.Id);
        foreach (var integralParameter in integralParameters)
        {
            var parameterViewModel = _parameterViewModelFactory.Create(integralParameter);
            if (integralValues.TryGetValue(integralParameter.Number, out var integralValue))
            {
                parameterViewModel.Value = integralValue;
            }
            IntegralParameters.Add(parameterViewModel);
        }
    }

    public void RequestDeviceUpdate(Device device)
    {
        DeviceUpdateRequested?.Invoke(this, device);
    }

    public void SetDataDisplayMode(DeviceViewMode mode)
    {
        if (DeviceViewMode == DeviceViewMode.Chart && mode == DeviceViewMode.Table)
        {
            if (DataChart.IntegrationSpanActive)
            {
                DataTable.UpdateCurrentDisplaySpan(DataChart.IntegrationSpanMinIndex, DataChart.IntegrationSpanMaxIndex);
            }
            else
            {
                DataTable.UpdateCurrentDisplaySpan(DataChart.CurrentDisplaySpanMinIndex, DataChart.CurrentDisplaySpanMaxIndex);
            }
        }

        DeviceViewMode = mode;
    }

    public async Task TryConnect()
    {
        try
        {
            _deviceConnection = _deviceConnectionFactory.Create(Device);

            _deviceConnection.OnConnectionError += async (sender, e) =>
            {
                _logger.Error(e.Exception, "Error while reading data from device");
                await TryDisconnect();
                ConnectionState = ConnectionState.Disconnected;
                ArchiveReadingProgress = 0;
                ArchiveReadingState = ArchiveReadingState.None;
            };

            _deviceConnection.OnConnectionStateChanged += (sender, e) =>
            {
                ConnectionState = e.State;
                _deviceIsEditableStore.UpdateDeviceIsEditable(Device.Id, e.State == ConnectionState.Disconnected);
            };

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
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while connecting to device");
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
                _logger.Error(e.Exception, "Error while reading data from file");
            };

            fileDataImporter.OnConnectionStateChanged += (sender, e) =>
            {
                ConnectionState = e.State;
                _deviceIsEditableStore.UpdateDeviceIsEditable(Device.Id, e.State == ConnectionState.Disconnected);
            };

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
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while reading data from file");
        }
    }

    public async Task SaveRealtimeParameters()
    {
        if (Device == default)
        {
            return;
        }

        var json = 
            JsonSerializer.Serialize(IntegralParameters.ToDictionary(x => x.Parameter.Number, x => x.Value)) +
            "|" +
            JsonSerializer.Serialize(CurrentParameters.ToDictionary(x => x.Parameter.Number, x => x.Value));

        await _deviceRepository.SetRealTimeValues(Device.Id, json);
    }


    private async void _deviceStore_Updated(object sender, Device device)
    {
        if (Device.Id == device.Id)
        {
            await Activate(device);
        }
    }

    private async void _deviceStore_Removed(object sender, Device device)
    {
        if (Device.Id == device.Id)
        {
            await TryDisconnect();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void _deviceIsEditableStore_DeviceIsEditableUpdated(object sender, Domain.Abstractions.Stores.Events.DeviceIsEditableArgs e)
    {
        if (Device.Id == e.DeviceId)
        {
            IsEditable = e.IsEditable;
        }
    }

    private void DataChart_OnIntegrationChanged(object sender, IntegrationChangedArgs e)
    {
        DataIntegration.UpdateValues(e);
    }
}
