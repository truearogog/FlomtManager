using FlomtManager.Core.Enums;

namespace FlomtManager.Core.Models
{
    public class Parameter
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }

        public required byte Number { get; set; }
        public required ParameterType ParameterType { get; set; }
        public required float Comma { get; set; }
        public required ushort ErrorMask { get; set; }
        public required byte IntegrationNumber { get; set; }
        public required string Name { get; set; }
        public required string Unit { get; set; }

        public int DeviceId { get; set; }
    }
}
