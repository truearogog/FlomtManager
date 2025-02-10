
namespace FlomtManager.Core.Entities;

public sealed class DataGroup : IEntity
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }

    public DateTime DateTime { get; set; }
    public required byte[] Data { get; set; }

    public int DeviceId { get; set; }
    public Device? Device { get; set; }
}
