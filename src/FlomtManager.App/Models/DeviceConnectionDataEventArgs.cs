namespace FlomtManager.App.Models
{
    public class DeviceConnectionDataEventArgs : EventArgs
    {
        public required IReadOnlyDictionary<byte, string> CurrentParameters { get; set; }
        public required IReadOnlyDictionary<byte, string> IntegralParameters { get; set; }
    }
}
