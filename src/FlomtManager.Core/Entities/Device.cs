using System.IO.Ports;
using FlomtManager.Core.Enums;

namespace FlomtManager.Core.Entities;

public sealed class Device : IEntity
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }

    public required string Name { get; set; }
    public string Address { get; set; }

    public ConnectionType ConnectionType { get; set; }
    public byte SlaveId { get; set; }

    public string PortName { get; set; }
    public int BaudRate { get; set; }
    public Parity Parity { get; set; }
    public int DataBits { get; set; }
    public StopBits StopBits { get; set; }

    public string IpAddress { get; set; }
    public int Port { get; set; }

    public int DeviceDefinitionId { get; set; }
    public DeviceDefinition DeviceDefinition { get; set; }
    public ICollection<Parameter> Parameters { get; set; }
    public ICollection<DataGroup> DataGroups { get; set; }
}
