using FlomtManager.Core.Models.Base;

namespace FlomtManager.Core.Models
{
    public class DataGroup : ModelBase
    {
        public DateTime Created { get; set; }

        public DateTime DateTime { get; set; }
        public required byte[] Data { get; set; }

        public int DeviceId { get; set; }
    }
}
