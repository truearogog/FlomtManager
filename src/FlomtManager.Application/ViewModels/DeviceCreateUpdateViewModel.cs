using System.Collections.ObjectModel;
using System.IO.Ports;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;
using FlomtManager.Framework.Helpers;
using ReactiveUI;
using Serilog;

namespace FlomtManager.Application.ViewModels;

internal sealed class DeviceCreateUpdateViewModel : ViewModel, IDeviceCreateUpdateViewModel
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceFormViewModelFactory _formViewModelFactory;
    private readonly ILogger _logger = Log.ForContext<DeviceCreateUpdateViewModel>();

    public event EventHandler CloseRequested;

    public IDeviceFormViewModel Form { get; set; }

    private string _errorMessage;
    public string ErrorMessage
    {
        get => _errorMessage;
        private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public ObservableCollection<ConnectionType> ConnectionTypes { get; set; } = new(Enum.GetValues<ConnectionType>());
    public ObservableCollection<string> PortNames { get; set; } = [];
    public ObservableCollection<int> BaudRates { get; set; } = [110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000];
    public ObservableCollection<Parity> Parities { get; set; } = new(Enum.GetValues<Parity>());
    public ObservableCollection<int> DataBits { get; set; } = [5, 6, 7, 8];
    public ObservableCollection<StopBits> StopBits { get; set; } = new(Enum.GetValues<StopBits>());

    public DeviceCreateUpdateViewModel(
        IDeviceRepository deviceRepository,
        IDeviceStore deviceStore,
        IDeviceFormViewModelFactory formViewModelFactory)
    {
        _deviceRepository = deviceRepository;
        _deviceStore = deviceStore;
        _formViewModelFactory = formViewModelFactory;

        Form = _formViewModelFactory.Create();

        RefreshPortNames();
    }

    public void SetDevice(Device device)
    {
        Form = _formViewModelFactory.Create(device);
        PortNames = [Form.PortName];
    }

    public void RefreshPortNames()
    {
        PortNames.Clear();

        var names = SerialPort.GetPortNames();
        foreach (var name in names)
        {
            PortNames.Add(name);
        }

        if (string.IsNullOrEmpty(Form.PortName))
        {
            Form.PortName = PortNames.FirstOrDefault() ?? string.Empty;
        }
    }

    public void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public async Task CreateDevice()
    {
        var validationErrors = ValidationHelper.Validate(Form);
        if (validationErrors.Any())
        {
            return;
        }

        try
        {
            var device = Form.GetDevice();
            var id = await _deviceRepository.Create(device);
            device = device with { Id = id };
            _deviceStore.Add(device);

            RequestClose();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Couldn't add new device.");
            ErrorMessage = "Couldn't add new device.";
        }
    }

    public async void UpdateDevice()
    {
        var validationErrors = ValidationHelper.Validate(Form);
        if (validationErrors.Any())
        {
            return;
        }

        try
        {
            var device = Form.GetDevice();
            await _deviceRepository.Update(device);
            _deviceStore.Update(device);

            RequestClose();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Couldn't edit device.");
            ErrorMessage = "Couldn't edit device.";
        }
    }
}
