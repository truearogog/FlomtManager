namespace FlomtManager.Modbus
{
    public class ModbusHelper
    {
        public static ushort GetCRC(ReadOnlySpan<byte> bytes)
        {
            ushort crcFull = 0xFFFF;
            char CRCLSB;

            for (int i = 0; i < bytes.Length; ++i)
            {
                crcFull = (ushort)(crcFull ^ bytes[i]);

                for (int j = 0; j < 8; ++j)
                {
                    CRCLSB = (char)(crcFull & 0x0001);
                    crcFull = (ushort)((crcFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                    {
                        crcFull = (ushort)(crcFull ^ 0xA001);
                    }
                }
            }
            return crcFull;
        }

        public static byte[] GetCRCBytes(ReadOnlySpan<byte> bytes)
        {
            var crcFull = GetCRC(bytes);
            return [(byte)(crcFull & 0xFF), (byte)((crcFull >> 8) & 0xFF)];
        }
    }
}
