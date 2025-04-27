using FlomtManager.Application.ViewModels;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;

namespace FlomtManager.Application.ViewModelFactories;

internal sealed class DeviceFormViewModelFactory(IDeviceRepository repository) : IDeviceFormViewModelFactory
{
    public async Task<IDeviceFormViewModel> Create()
    {
        var lastDevice = await repository.GetLastCreated();
        if (lastDevice != default)
        {
            return new DeviceFormViewModel
            {
                ConnectionType = lastDevice.ConnectionType,
                PortName = lastDevice.PortName,
                BaudRate = lastDevice.BaudRate,
                Parity = lastDevice.Parity,
                DataBits = lastDevice.DataBits,
                StopBits = lastDevice.StopBits,
                IpAddress = lastDevice.IpAddress,
                Port = lastDevice.Port,
            };
        }

        return new DeviceFormViewModel();
    }

    public IDeviceFormViewModel Create(Device device)
    {
        return new DeviceFormViewModel()
        {
            Id = device.Id,
            Name = device.Name,
            Address = device.Address,

            ConnectionType = device.ConnectionType,
            SlaveId = device.SlaveId,
            DataReadEnabled = device.DataReadEnabled,
            DataReadInterval = TimeSpan.FromTicks(device.DataReadIntervalTicks),

            PortName = device.PortName,
            BaudRate = device.BaudRate,
            Parity = device.Parity,
            DataBits = device.DataBits,
            StopBits = device.StopBits,

            IpAddress = device.IpAddress,
            Port = device.Port,
        };
    }
}
