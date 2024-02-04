using FlomtManager.Core.Enums;
using FlomtManager.Data.EF.Entities.Base;
using System.IO.Ports;

namespace FlomtManager.Data.EF.Entities
{
    public sealed class DeviceEntity : EntityBase
    {
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
        public DeviceDefinitionEntity DeviceDefinition { get; set; }
        public ICollection<ParameterEntity> Parameters { get; set; }
    }
}
