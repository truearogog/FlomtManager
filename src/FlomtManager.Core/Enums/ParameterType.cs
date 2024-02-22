using FlomtManager.Core.Attributes;

namespace FlomtManager.Core.Enums
{
    public enum ParameterType
    {
        // b 0...2 comma position 0=0, 1=0.0, 2=0.00, 3=0.000, 4=0.0000, 5x, 6x, 7=*10
        // b 3..6 type
        [Size(2)]
        S16C, // 0000.XXX(00-07) - S16C0...S16C7 - fixed comma signed
        [Size(2)]
        U16C, // 0001.XXX(08-15) - U16C0...U16C7 - fixed comma unsigned
        [Size(2)]
        FS16C, // 0010.XXX(16-23) - FS16?0...FS16?7 - float 16 signed
        [Size(2)]
        FU16C, // 0011.XXX(24-31) - FU16?0...FU16?7 - float 16 unsigned
        [Size(4)]
        S32C, // 0100.XXX(32-39) - S32C0...S32C7 - 32 bit value fixed comma
        [Size(4)]
        S32CD1, // 0101.XXX(40-47) - S32C0D1...S32C7D1 - 32 bit value ignore 1 last digits
        [Size(4)]
        S32CD2, // 0110.XXX(48-55) - S32C0D2...S32C7D2 - 32 bit value ignore 2 last digits
        [Size(4)]
        S32CD3, // 0111.XXX(56-63) - S32C0D3...S32C7D3 - 32 bit value ignore 3 last digits
        [Size(2)]
        [Hide]
        Error, // 64 - 16 bit unsigned Errors
        [Size(4)]
        WorkingTimeInSeconds, // 65 - 32 bit T working time in seconds (HHHH:MM:SS)
        [Size(2)]
        WorkingTimeInSecondsInArchiveInterval, // 66 - 16 bit unsigned working time in seconds in archive interval
        [Size(2)]
        WorkingTimeInMinutesInArchiveInterval, // 67 - 16 bit unsigned Working minutes in the archive interval
        [Size(2)]
        WorkingTimeInHoursInArchiveInterval, // 68 - 16 bit unsigned Working hours in the archived interval
        [Size(6)]
        Time, // 69 - 8 bit * 6 SS:MM:HH DD.MM.YY 
        [Size(4)]
        SecondsSince2000, // 70 - U32 seconds since 2000 year
    }
}