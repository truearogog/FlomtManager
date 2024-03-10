using FlomtManager.Core.Enums;
using FlomtManager.Core.Models.Base;

namespace FlomtManager.Core.Models
{
    public class Parameter : ModelBase
    {
        public DateTime Created { get; set; }

        public required byte Number { get; set; }
        public required ParameterType ParameterType { get; set; }
        public required byte Comma { get; set; }
        public required ushort ErrorMask { get; set; }
        public required byte IntegrationNumber { get; set; }
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public required string Color { get; set; }

        public int DeviceId { get; set; }
    }
}
