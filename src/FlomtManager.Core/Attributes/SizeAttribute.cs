namespace FlomtManager.Core.Attributes
{
    public class SizeAttribute : Attribute
    {
        public byte Size { get; }

        public SizeAttribute(byte size)
        {
            Size = size;
        }
    }
}
