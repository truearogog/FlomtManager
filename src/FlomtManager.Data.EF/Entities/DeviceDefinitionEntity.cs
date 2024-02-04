using FlomtManager.Data.EF.Entities.Base;

namespace FlomtManager.Data.EF.Entities
{
    public sealed class DeviceDefinitionEntity : EntityBase
    {
        public ushort ParameterDefinitionStart { get; set; }
        public ushort ParameterDefinitionNumber { get; set; }

        public ushort DescriptionStart { get; set; }
        public ushort ProgramVersionStart { get; set; }

        public ushort CurrentParameterLineDefinitionStart { get; set; }
        public byte[] CurrentParameterLineDefinition { get; set; }
        public byte CurrentParameterLineLength { get; set; }
        public byte CurrentParameterLineNumber { get; set; }
        public ushort CurrentParameterLineStart { get; set; }

        public ushort IntegralParameterLineDefinitionStart { get; set; }
        public byte[] IntegralParameterLineDefinition { get; set; }
        public byte IntegralParameterLineLength { get; set; }
        public byte IntegralParameterLineNumber { get; set; }
        public ushort IntegralParameterLineStart { get; set; }

        public ushort CRC { get; set; }

        public int DeviceId { get; set; }
        public DeviceEntity Device { get; set; }
    }
}
