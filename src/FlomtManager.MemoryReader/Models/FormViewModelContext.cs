using System.Text.Json.Serialization;
using FlomtManager.MemoryReader.ViewModels;

namespace FlomtManager.MemoryReader.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    [JsonSerializable(typeof(FormViewModel))]
    internal partial class FormViewModelContext : JsonSerializerContext
    {
    }
}
