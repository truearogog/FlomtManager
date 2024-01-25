namespace FlomtManager.Data.EF.Entities
{
    public sealed class DeviceEntity
    {
        public int Id { get; set; }
        public string SerialCode { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string MeterNr { get; set; }

        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
    }
}
