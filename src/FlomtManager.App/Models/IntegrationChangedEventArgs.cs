namespace FlomtManager.App.Models
{
    public class IntegrationChangedEventArgs : EventArgs
    {
        public required DateTime IntegrationStart { get; set; }
        public required DateTime IntegrationEnd { get; set; }
        public required IEnumerable<(byte, float)> IntegrationData { get; set; }
    }
}
