using FlomtManager.Core.Enums;

namespace FlomtManager.Core.Entities;

public sealed class Parameter : IEntity
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }

    public required byte Number { get; set; }
    public required ParameterType ParameterType { get; set; }
    public required byte Comma { get; set; }
    public required ushort ErrorMask { get; set; }
    public required byte IntegrationNumber { get; set; }
    public required string Name { get; set; }
    public required string Unit { get; set; }
    public required string Color { get; set; }

    public int DeviceId { get; set; }
    public Device Device { get; set; }
}
