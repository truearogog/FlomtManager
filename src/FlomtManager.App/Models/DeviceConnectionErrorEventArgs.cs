namespace FlomtManager.App.Models
{
    public class DeviceConnectionErrorEventArgs : EventArgs
    {
        public required Exception Exception { get; set; }
    }
}
