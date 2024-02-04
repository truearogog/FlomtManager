namespace FlomtManager.Core.Models
{
    public class Parameter
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }

        public byte Number { get; set; }
        public byte IntegrationNumber { get; set; }
        public ushort ErrorMask { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }

        public int DeviceId { get; set; }
    }
}
