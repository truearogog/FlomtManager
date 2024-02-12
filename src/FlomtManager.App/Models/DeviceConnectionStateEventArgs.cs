using FlomtManager.Core.Enums;

namespace FlomtManager.App.Models
{
    public class DeviceConnectionStateEventArgs : EventArgs
    {
        public required int DeviceId { get; set; }
        public required ConnectionState ConnectionState { get; set; }
    }
}
