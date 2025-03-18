using MemoryPack;

namespace FlomtManager.Framework.Extensions
{
    public static class ObjectExtensions
    {
        public static byte[] GetBytes<T>(this T obj)
        {
            return MemoryPackSerializer.Serialize(obj);
        }
    }
}
