using Avalonia.Controls;

namespace FlomtManager.App.Stores
{
    public class DeviceWindowStore
    {
        private Dictionary<int, Window> _deviceWindows = [];

        public bool TryGetWindow(int deviceId, out Window? window)
        {
            var result = _deviceWindows.TryGetValue(deviceId, out var _window);
            window = _window;
            return result;
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
