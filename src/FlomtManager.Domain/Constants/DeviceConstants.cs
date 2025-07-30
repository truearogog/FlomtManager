namespace FlomtManager.Domain.Constants
{
    public static class DeviceConstants
    {
        public const byte BYTES_PER_REGISTER = 2;

        public const int MEMORY_SIZE = MEMORY_SIZE_BYTES / BYTES_PER_REGISTER;
        public const int MEMORY_SIZE_BYTES = 64 * 1024;

        public const ushort MAX_PARAMETER_COUNT = 22;
        public const ushort PARAMETER_SIZE = 8;
        public const ushort PARAMETER_SIZE_BYTES = PARAMETER_SIZE * BYTES_PER_REGISTER;

        public const ushort DEVICE_NUMBER_START = 100 / BYTES_PER_REGISTER;
        public const ushort DEVICE_NUMBER_SIZE = 1;

        public const ushort CURRENT_DATETIME_START = 102 / BYTES_PER_REGISTER;
        public const ushort CURRENT_DATETIME_SIZE = 3;

        public const ushort MEMORY_DEFINITION_START = 116 / BYTES_PER_REGISTER;
        public const ushort MEMORY_DEFINITION_LENGTH = 42;

        public static ushort ToRegisters(this ushort address)
        {
            return (ushort)(address / BYTES_PER_REGISTER);
        }

        public static ushort ToBytes(this ushort address)
        {
            return (ushort)(address * BYTES_PER_REGISTER);
        }
    }
}
