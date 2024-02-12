namespace FlomtManager.App.Models
{
    public class DeviceConnectionErrorEventArgs : EventArgs
    {
        public required int DeviceId { get; set; }
        public required Exception Exception { get; set; }
    }
}
