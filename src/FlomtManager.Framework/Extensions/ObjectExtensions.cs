using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlomtManager.Framework.Extensions
{
    public static class ObjectExtensions
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = null,
            WriteIndented = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static byte[] SerializeBytes(this object obj)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj, JsonSerializerOptions));
        }
    }
}
