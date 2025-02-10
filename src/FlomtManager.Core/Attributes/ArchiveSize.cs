namespace FlomtManager.Core.Attributes;

[AttributeUsage(AttributeTargets.All)]
public sealed class ArchiveSize(byte size) : Attribute
{
    public byte Size { get; } = size;
}
