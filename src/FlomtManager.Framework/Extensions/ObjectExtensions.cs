using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace FlomtManager.Framework.Extensions
{
    public static class ObjectExtensions
    {
        public static byte[]? GetBytes(this object obj)
        {
            if (obj == null)
                return default;

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj, GetJsonSerializerOptions()));
        }

        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                PropertyNamingPolicy = null,
                WriteIndented = true,
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
        }
    }
}
