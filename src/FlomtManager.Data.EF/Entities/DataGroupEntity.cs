using FlomtManager.Data.EF.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace FlomtManager.Data.EF.Entities
{
    [Index(nameof(DateTime), nameof(DeviceId), IsUnique = true)]
    public sealed class DataGroupEntity : EntityBase
    {
        public DateTime DateTime { get; set; }
        public required byte[] Data { get; set; }

        public int DeviceId { get; set; }
        public DeviceEntity? Device { get; set; }
    }
}
