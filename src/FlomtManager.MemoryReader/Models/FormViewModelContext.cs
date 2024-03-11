using FlomtManager.MemoryReader.ViewModels;
using System.Text.Json.Serialization;

namespace FlomtManager.MemoryReader.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    [JsonSerializable(typeof(FormViewModel))]
    internal partial class FormViewModelContext : JsonSerializerContext
    {
    }
}
