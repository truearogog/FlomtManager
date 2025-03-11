using System.Collections.ObjectModel;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using FlomtManager.App.Enums;
using FlomtManager.Core.DeviceConnection;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Events;
using FlomtManager.Core.Models;
using FlomtManager.Core.Providers;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Stores;
using FlomtManager.Core.ViewModels;
using FlomtManager.Modbus;
using ReactiveUI;

namespace FlomtManager.App.ViewModels
{
    internal class DeviceViewModel : ViewModel
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IDeviceStore _deviceStore;
        private readonly IDeviceConnectionFactory _connectionViewModelFactory;
        private readonly IParameterRepository _parameterRepository;

        public event EventHandler CloseRequested;
        public event EventHandler<Device> DeviceUpdateRequested;
        public event EventHandler<(NotificationType, string)> NotificationRequested;
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

        public ObservableCollection<ParameterViewModel> CurrentParameters { get; set; } = [];
        public ObservableCollection<ParameterViewModel> IntegralParameters { get; set; } = [];

        #region Device Connection

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

        public IDeviceConnection Connection { get; set; }

        #endregion

        public DataGroupChartViewModel DataGroupChart { get; set; }
        public DataGroupTableViewModel DataGroupTable { get; set; }
        public DataGroupIntegrationViewModel DataGroupIntegration { get; set; }

        public DeviceViewModel(
            IDateTimeProvider dateTimeProvider,
            IDeviceStore deviceStore,
            IDeviceConnectionFactory connectionViewModelFactory,
            IParameterRepository parameterRepository,
            DataGroupChartViewModel dataGroupChartViewModel,
            DataGroupTableViewModel dataGroupTableViewModel,
            DataGroupIntegrationViewModel dataGroupIntegrationViewModel)
        {
            _dateTimeProvider = dateTimeProvider;
            _deviceStore = deviceStore;
            _connectionViewModelFactory = connectionViewModelFactory;
            _parameterRepository = parameterRepository;

            _deviceStore.DeviceUpdated += _DeviceUpdated;
            _deviceStore.DeviceRemoved += _DeviceRemoved;
            
            DataGroupChart = dataGroupChartViewModel;
            DataGroupChart.OnIntegrationChanged += _OnIntegrationChanged;

            DataGroupTable = dataGroupTableViewModel;

            DataGroupIntegration = dataGroupIntegrationViewModel;
        }

        public async Task SetDevice(Device device)
        {
            Device = device;
            await DataGroupChart.SetDevice(device);
            await DataGroupTable.SetDevice(device);
            await DataGroupIntegration.SetDevice(device);
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
                CurrentParameters.Add(new() { Parameter = currentParameter });
            }

            IntegralParameters.Clear();
            var integralParameters = await _parameterRepository.GetIntegralParametersByDeviceId(Device.Id);
            foreach (var integralParameter in integralParameters)
            {
                IntegralParameters.Add(new() { Parameter = integralParameter });
            }
        }

        public void RequestDeviceUpdate(Device device)
        {
            DeviceUpdateRequested?.Invoke(this, device);
        }

        private async void _DeviceUpdated(object sender, Device device)
        {
            if (Device.Id == device.Id)
            {
                await SetDevice(device);
            }
        }

        private void _DeviceRemoved(object sender, Device device)
        {
            if (Device.Id == device.Id)
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetDataDisplayMode(ArchiveDisplayMode mode)
        {
            ArchiveDisplayMode = mode;
        }

        public async Task TryConnect()
        {
            try
            {
                Connection = _connectionViewModelFactory.Create(Device);

                Connection.OnConnectionError += async (sender, e) =>
                {
                    RequestNotification(NotificationType.Error, e.Exception.Message);
                    await TryDisconnect();
                };

                Connection.OnConnectionStateChanged += (sender, e) => ConnectionState = e.State;

                Connection.OnConnectionDataChanged += (sender, e) =>
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

                Connection.OnConnectionArchiveReadingStateChanged += async (sender, e) =>
                {
                    ArchiveReadingState = e.State;
                    if (e.State == ArchiveReadingState.Complete)
                    {
                        LastTimeArchiveRead = _dateTimeProvider.Now;
                        if (e.LinesRead > 0)
                        {
                            await DataGroupChart.UpdateData();
                            await DataGroupTable.UpdateData();
                        }
                    }
                };

                Connection.OnConnectionArchiveReadingProgressChangedArgs += (sender, e) =>
                {
                    ArchiveReadingProgress = (int)((double)e.Progress / e.MaxProgress * 100);
                };

                await Connection.Connect();
            }
            catch (ModbusException ex)
            {
                RequestNotification(NotificationType.Error, ex.Message);
                await (Connection?.DisposeAsync() ?? ValueTask.CompletedTask);
                Connection = null;
            }
        }

        public async Task TryDisconnect()
        {
            await (Connection?.Disconnect() ?? Task.CompletedTask);
            await (Connection?.DisposeAsync() ?? ValueTask.CompletedTask);
            Connection = null;
        }

        private void _OnIntegrationChanged(object sender, IntegrationChangedArgs e)
        {
            DataGroupIntegration.UpdateValues(e);
        }

        //private CancellationTokenSource _connectionCancellationTokenSource;
        //public async void TryConnect()
        //{
        //    try
        //    {
        //        if (!Device.HasValue)
        //        {
        //            throw new ArgumentNullException(nameof(Device));
        //        }
        //        ArgumentNullException.ThrowIfNull(DeviceConnection);
        //        _connectionCancellationTokenSource = new();
        //        await DeviceConnection.Connect(Device, _connectionCancellationTokenSource.Token);
        //        UpdateParameters();
        //    }
        //    catch (ModbusException ex)
        //    {
        //        RequestNotification(NotificationType.Error, ex.Message);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        RequestNotification(NotificationType.Warning, "Connection cancelled.");
        //    }
        //    catch
        //    {
        //        RequestNotification(NotificationType.Error, "Connection error.");
        //    }
        //}

        //public void TryCancelConnect()
        //{
        //    ArgumentNullException.ThrowIfNull(_connectionCancellationTokenSource);
        //    _connectionCancellationTokenSource.Cancel();
        //}

        //public async void TryDisconnect()
        //{
        //    _connectionCancellationTokenSource?.Cancel();
        //    ArgumentNullException.ThrowIfNull(DeviceConnection);
        //    await DeviceConnection.Disconnect();
        //}

        //public async void ReadArchivesFromDevice()
        //{
        //    ArgumentNullException.ThrowIfNull(DeviceConnection);
        //    await DeviceConnection.ReadArchivesFromDevice();
        //    UpdateParameters();
        //    UpdateData();
        //    DataGroupIntegration.Device = Device;
        //}

        //public void ReadArchivesFromFile()
        //{
        //    ReadFromFileRequested?.Invoke(this, EventArgs.Empty);
        //}

        //public async Task ReadArchivesFromFile(IStorageFile storageFile)
        //{
        //    ArgumentNullException.ThrowIfNull(Device);
        //    ArgumentNullException.ThrowIfNull(DeviceConnection);
        //    await DeviceConnection.ReadArchivesFromFile(Device, storageFile);
        //    UpdateParameters();
        //    UpdateData();
        //    DataGroupIntegration.Device = Device;
        //}

        //private void UpdateData()
        //{
        //    DataGroupChart.UpdateData();
        //}

        private void RequestNotification(NotificationType type, string message)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                NotificationRequested?.Invoke(this, (type, message));
            });
        }
    }
}
