using FlomtManager.Core.Enums;
using FlomtManager.Data.EF.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace FlomtManager.Data.EF.Entities
{
    public sealed class ParameterEntity : EntityBase
    {
        public required byte Number { get; set; }
        public required ParameterType ParameterType { get; set; }
        public required byte Comma { get; set; }
        public required ushort ErrorMask { get; set; }
        public required byte IntegrationNumber { get; set; }
        [MaxLength(4)]
        public required string Name { get; set; }
        [MaxLength(6)]
        public required string Unit { get; set; }
        public required string Color { get; set; }

        public required int DeviceId { get; set; }
        public DeviceEntity? Device { get; set; }
    }
}
