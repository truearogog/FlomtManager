namespace FlomtManager.Domain.Constants
{
    public class DeviceConstants
    {
        public const int MEMORY_SIZE_BYTES = 64 * 1024;
        public const int MEMORY_SIZE_REGISTERS = MEMORY_SIZE_BYTES / 2;

        public const byte MAX_PARAMETER_COUNT = 22;
        public const byte PARAMETER_SIZE = 16;

        public const byte CURRENT_DATETIME_START = 102;
        public const byte CURRENT_DATETIME_SIZE_REGISTERS = 3;

        public const ushort MEMORY_DEFINITION_START = 116;
        public const ushort MEMORY_DEFINITION_LENGTH_REGISTERS = 42;
        public const ushort MEMORY_DEFINITION_LENGTH_BYTES = MEMORY_DEFINITION_LENGTH_REGISTERS * 2;
    }
}
