using FlomtManager.Application.ViewModels;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;

namespace FlomtManager.Application.ViewModelFactories;

internal sealed class DeviceFormViewModelFactory : IDeviceFormViewModelFactory
{
    public IDeviceFormViewModel Create()
    {
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
