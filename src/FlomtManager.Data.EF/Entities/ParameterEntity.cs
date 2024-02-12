using FlomtManager.Data.EF.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace FlomtManager.Data.EF.Entities
{
    public sealed class ParameterEntity : EntityBase
    {
        public byte Number { get; set; }
        public byte IntegrationNumber { get; set; }
        public ushort ErrorMask { get; set; }
        [MaxLength(4)]
        public required string Name { get; set; }
        [MaxLength(6)]
        public required string Unit { get; set; }

        public int DeviceId { get; set; }
        public DeviceEntity? Device { get; set; }
    }
}
