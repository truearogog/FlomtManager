using System.Runtime.InteropServices;

namespace FlomtManager.Modbus
{
    public static class CRCHelper
    {
        private static readonly ushort[] CrcTable = GenerateCrcTable();

        public static ushort GetCRC(ReadOnlySpan<byte> data)
        {
            ushort crc = 0xFFFF;

            foreach (byte b in data)
            {
                crc = (ushort)((crc >> 8) ^ CrcTable[(crc ^ b) & 0xFF]);
            }

            return crc;
        }

        public static ushort GetCRC(ReadOnlySpan<ushort> registers)
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(registers);
            return GetCRC(bytes);
        }

        public static byte[] GetCRCBytes(ReadOnlySpan<byte> bytes)
        {
            ushort crc = GetCRC(bytes);
            return [(byte)(crc & 0xFF), (byte)(crc >> 8)];
        }

        private static ushort[] GenerateCrcTable()
        {
            ushort[] table = new ushort[256];

            for (ushort i = 0; i < 256; i++)
            {
                ushort crc = i;
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                }
                table[i] = crc;
            }

            return table;
        }
    }
}
