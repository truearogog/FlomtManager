using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;
using ReactiveUI;
using Serilog;

namespace FlomtManager.Application.ViewModels;

internal sealed class DeviceDeleteViewModel(IDeviceRepository deviceRepository, IDeviceStore deviceStore) : ViewModel, IDeviceDeleteViewModel
{
    private readonly ILogger _logger = Log.ForContext<DeviceDeleteViewModel>();

    public event EventHandler CloseRequested;

    private Device _device;
    public Device Device
    {
        get => _device;
        private set => this.RaiseAndSetIfChanged(ref _device, value);
    }

    private string _errorMessage;
    public string ErrorMessage
    {
        get => _errorMessage;
        private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    private bool _canDelete = false;
    public bool CanDelete
    {
        get => _canDelete;
        set => this.RaiseAndSetIfChanged(ref _canDelete, value);
    }

    private int _canDeleteCount;
    public int CanDeleteCount
    {
        get => _canDeleteCount;
        set => this.RaiseAndSetIfChanged(ref _canDeleteCount, value);
    }

    private const int DeleteDelay = 3;

    public void SetDevice(Device device)
    {
        Device = device;
        Task.Run(async () =>
        {
            for (var i = DeleteDelay; i > 0; --i)
            {
                CanDeleteCount = i;
                await Task.Delay(1000);
            }
            CanDelete = true;
        });
    }

    public void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public async void DeleteDevice()
    {
        if (!CanDelete)
        {
            return;
        }

        try
        {
            await deviceRepository.Delete(Device.Id);
            deviceStore.Remove(Device);

            RequestClose();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Couldn't delete device.");
            ErrorMessage = "Couldn't delete device.";
        }
    }
}
