using FlomtManager.Core.Enums;
using System.IO.Ports;

namespace FlomtManager.Core.Models
{
    public class Device
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        public string SerialCode { get; set; }
        public string Name { get; set; }
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
    }
}
