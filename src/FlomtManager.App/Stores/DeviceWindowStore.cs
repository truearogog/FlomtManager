using Avalonia.Controls;

namespace FlomtManager.App.Stores
{
    public class DeviceWindowStore
    {
        private readonly Dictionary<int, Window> _deviceWindows = [];

        public bool TryGetWindow(int deviceId, out Window window)
        {
            return _deviceWindows.TryGetValue(deviceId, out window);
        }

        public void AddWindow(int deviceId, Window window)
        {
            _deviceWindows.Add(deviceId, window);
        }

        public bool RemoveWindow(int deviceId)
        {
            return _deviceWindows.Remove(deviceId);
        }
    }
}
