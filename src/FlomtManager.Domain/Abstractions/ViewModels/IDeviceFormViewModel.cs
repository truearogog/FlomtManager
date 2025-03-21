using System.IO.Ports;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IDeviceFormViewModel : IViewModel
{
    int Id { get; set; }

    string Name { get; set; }
    string Address { get; set; }

    ConnectionType ConnectionType { get; set; }
    byte SlaveId { get; set; }
    TimeSpan DataReadInterval { get; set; }

    string PortName { get; set; }
    int BaudRate { get; set; }
    Parity Parity { get; set; }
    int DataBits { get; set; }
    StopBits StopBits { get; set; }

    string IpAddress { get; set; }
    int Port { get; set; }

    Device GetDevice();
}
