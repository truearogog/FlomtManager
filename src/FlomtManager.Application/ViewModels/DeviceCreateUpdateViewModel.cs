﻿using System.Collections.ObjectModel;
using System.IO.Ports;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;
using FlomtManager.Framework.Helpers;
using ReactiveUI;

namespace FlomtManager.Application.ViewModels;

internal sealed class DeviceCreateUpdateViewModel(
    IDeviceRepository deviceRepository, 
    IDeviceStore deviceStore,
    IDeviceFormViewModelFactory formViewModelFactory) : ViewModel, IDeviceCreateUpdateViewModel
{
    public event EventHandler CloseRequested;

    public IDeviceFormViewModel Form { get; set; } = formViewModelFactory.Create();

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

    public void SetDevice(Device device)
    {
        Form = formViewModelFactory.Create(device);
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
        Form.PortName = PortNames.FirstOrDefault() ?? string.Empty;
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
            var id = await deviceRepository.Create(device);
            device = device with { Id = id };
            deviceStore.Add(device);

            RequestClose();
        }
        catch (Exception)
        {
            // todo: log error
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
            await deviceRepository.Update(device);
            deviceStore.Update(device);

            RequestClose();
        }
        catch (Exception)
        {
            // todo: log error
            ErrorMessage = "Couldn't edit device.";
        }
    }
}
