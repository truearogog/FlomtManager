namespace FlomtManager.Core.Models
{
    public class Parameter
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }

        public byte Number { get; set; }
        public byte IntegrationNumber { get; set; }
        public ushort ErrorMask { get; set; }
        public required string Name { get; set; }
        public required string Unit { get; set; }

        public int DeviceId { get; set; }
    }
}
