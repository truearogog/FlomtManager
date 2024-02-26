using Avalonia.Platform.Storage;

namespace FlomtManager.App
{
    public static class FlomtFilePickerFileTypes
    {
        public static FilePickerFileType Hex { get; } = new("Hex")
        {
            Patterns = new[] { "*.hex" },
            MimeTypes = new[] { "text/plain" }
        };
    }
}
