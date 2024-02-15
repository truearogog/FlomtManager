namespace FlomtManager.Core.Attributes
{
    public class SizeAttribute(byte size) : Attribute
    {
        public byte Size { get; } = size;
    }
}
