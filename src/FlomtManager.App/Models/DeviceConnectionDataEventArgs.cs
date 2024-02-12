namespace FlomtManager.App.Models
{
    public class DeviceConnectionDataEventArgs : EventArgs
    {
        public required int DeviceId { get; set; }
        public required IDictionary<int, string> CurrentParameters { get; set; }
        public required IDictionary<int, string> IntegralParameters { get; set; }
    }
}
