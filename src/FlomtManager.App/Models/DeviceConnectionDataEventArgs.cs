namespace FlomtManager.App.Models
{
    public class DeviceConnectionDataEventArgs : EventArgs
    {
        public required IReadOnlyDictionary<byte, ParameterValue> CurrentParameters { get; set; }
        public required IReadOnlyDictionary<byte, ParameterValue> IntegralParameters { get; set; }
    }
}
