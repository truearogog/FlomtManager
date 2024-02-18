using FlomtManager.Core.Constants;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Services;
using FlomtManager.Framework.Extensions;
using FlomtManager.Modbus;
using System.Text;

namespace FlomtManager.Services.Services
{
    internal class ModbusService : IModbusService
    {
        public async Task<DeviceDefinition> ReadDeviceDefinition(IModbusProtocol modbusProtocol, byte slaveId, CancellationToken cancellationToken)
        {
            var registers = await modbusProtocol.ReadRegistersAsync(slaveId, 116, 42, cancellationToken: cancellationToken);
            var deviceDefinition = new DeviceDefinition
            {
                ParameterDefinitionStart = registers[0],
                ParameterDefinitionNumber = registers[1],
                DescriptionStart = registers[2],
                ProgramVersionStart = registers[3],

                CurrentParameterLineDefinitionStart = registers[4],
                CurrentParameterLineNumber = (byte)registers[5],
                CurrentParameterLineLength = (byte)(registers[5] >> 8),
                CurrentParameterLineStart = registers[6],

                IntegralParameterLineDefinitionStart = registers[7],
                IntegralParameterLineNumber = (byte)registers[8],
                IntegralParameterLineLength = (byte)(registers[8] >> 8),
                IntegralParameterLineStart = registers[9],
            };

            var currentParameterDefinition = await modbusProtocol.ReadRegistersBytesAsync(
                slaveId, deviceDefinition.CurrentParameterLineDefinitionStart, deviceDefinition.CurrentParameterLineNumber, cancellationToken: cancellationToken);
            deviceDefinition.CurrentParameterLineDefinition = currentParameterDefinition;

            var integralParameterDefinition = await modbusProtocol.ReadRegistersBytesAsync(
                slaveId, deviceDefinition.IntegralParameterLineDefinitionStart, deviceDefinition.IntegralParameterLineNumber, cancellationToken: cancellationToken);
            deviceDefinition.IntegralParameterLineDefinition = integralParameterDefinition;

            var bytes = deviceDefinition.GetBytes();
            deviceDefinition.CRC = ModbusHelper.GetCRC(bytes);

            return deviceDefinition;
        }

        /*
        200.352 [C] "All parameters definition" 22 parameters * 16 one block length)
            X... Description of 1 parameter Bytes: (16 bytes per parameter)
            0 - Assigning a number to the parameter 0 - disabled
                        * this number will be used when describing parameter blocks
            1 - Assign parameter number after integration (0 - ordinary parameter) 
                        * for example: Q(m3/h)(INT16) after integration converted to V(m3)(INT32)
            2,3 - UINT16 error mask
            4,5 - reserved 
            6...9 -'Q',0,0,0 - symbolic name of the parameter - 4 characters max or up to 0
            10...15 - 'm3/h',0,0 - parameter units - 6 characters max or up to 0
        */
        public async Task<IEnumerable<Parameter>> ReadParameterDefinitions(IModbusProtocol modbusProtocol, byte slaveId, DeviceDefinition deviceDefinition, CancellationToken ct)
        {
            var bytes = await modbusProtocol.ReadRegistersBytesAsync(slaveId, deviceDefinition.ParameterDefinitionStart, DeviceConstants.MAX_PARAMETER_COUNT * 16 / 2, cancellationToken: ct);

            var parameters = new List<Parameter>();
            for (var i = 0; i < DeviceConstants.MAX_PARAMETER_COUNT; ++i)
            {
                var parameterBytes = bytes.Skip(i * 16).Take(16).ToArray();
                if (parameterBytes[0] == 0)
                    continue;

                var parameter = new Parameter
                {
                    Number = parameterBytes[0],
                    IntegrationNumber = parameterBytes[1],
                    ErrorMask = (ushort)(parameterBytes[2] + parameterBytes[3] * 256),
                    Name = Encoding.ASCII.GetString(parameterBytes.Skip(6).TakeWhile(x => x != '\0').ToArray()),
                    Unit = Encoding.ASCII.GetString(parameterBytes.Skip(10).TakeWhile(x => x != '\0').ToArray()),
                };

                parameters.Add(parameter);
            }
            return parameters;
        }

        /*
        b 0...2 comma position 0=0, 1=0.0, 2=0.00, 3=0.000, 4=0.0000, 7=*10
        b 3...6 0000.XXX(00-07) - S16C0...S16C7 - fixed comma signed,
                0001.XXX(08-15) - U16C0...U16C7 - fixed comma unsigned,
                0010.XXX(16-23) - FS16?0...FS16?7 - float 16 signed,
                0011.XXX(24-31) - FU16?0...FU16?7 - float 16 unsigned,
                0100.XXX(32-39) - S32C0...S32C7 - 32 bit value fixed comma
                0101.XXX(40-47) - S32C0D1...S32C7D1 - 32 bit value ignore 1 last digits
                0110.XXX(48-55) - S32C0D2...S32C7D2 - 32 bit value ignore 2 last digits
                0111.XXX(56-63) - S32C0D3...S32C7D3 - 32 bit value ignore 3 last digits
                64 - 16 bit unsigned Errors
                65 - 32 bit T working time in seconds (HHHH:MM:SS)
                66 - 16 bit unsigned working time in seconds in archive interval
                67 - 16 bit unsigned Working minutes in the archive interval
                68 - 16 bit unsigned Working hours in the archived interval
                69 - 8 bit * 6 SS:MM:HH DD.MM.YY
                70 - U32 seconds since 2000 year
                71 - 127 - reserved
        */
        public (ParameterType type, float comma) ParseParameterTypeByte(byte parameterTypeByte)
        {
            return parameterTypeByte switch
            {
                <= 7 => (ParameterType.S16C, GetCommaFromByte(parameterTypeByte)),
                >= 8 and <= 15 => (ParameterType.U16C, GetCommaFromByte(parameterTypeByte)),
                >= 16 and <= 23 => (ParameterType.FS16C, GetCommaFromByte(parameterTypeByte)),
                >= 24 and <= 31 => (ParameterType.FU16C, GetCommaFromByte(parameterTypeByte)),
                >= 32 and <= 39 => (ParameterType.S32C, GetCommaFromByte(parameterTypeByte)),
                >= 40 and <= 47 => (ParameterType.S32CD1, GetCommaFromByte(parameterTypeByte)),
                >= 48 and <= 55 => (ParameterType.S32CD2, GetCommaFromByte(parameterTypeByte)),
                >= 56 and <= 63 => (ParameterType.S32CD3, GetCommaFromByte(parameterTypeByte)),
                64 => (ParameterType.Error, 1),
                65 => (ParameterType.WorkingTimeInSeconds, 1),
                66 => (ParameterType.WorkingTimeInSecondsInArchiveInterval, 1),
                67 => (ParameterType.WorkingTimeInMinutesInArchiveInterval, 1),
                68 => (ParameterType.WorkingTimeInHoursInArchiveInterval, 1),
                69 => (ParameterType.Time, 1),
                70 => (ParameterType.SecondsSince2000, 1),
                _ => throw new NotSupportedException(),
            };
        }

        private static float GetCommaFromByte(byte commaByte)
        {
            commaByte &= 0x7;
            return commaByte switch
            {
                0 => 1,
                >= 1 and <= 4 => MathF.Pow(10, -commaByte),
                7 => 10,
                _ => throw new NotSupportedException()
            };
        }

        private float ParseBytesToParameter(ReadOnlySpan<byte> bytes, ParameterType parameterType, float comma)
        {
            return comma * (parameterType switch
            {
                ParameterType.S16C => BitConverter.ToInt16(bytes),
                ParameterType.U16C => BitConverter.ToUInt16(bytes),
                ParameterType.FS16C => (BitConverter.ToUInt16(bytes) & 0x3FFF) * (((bytes[0] >> 6) & 1) == 0 ? 1 : -1) * MathF.Pow(10, -(bytes[1] >> 7)),
                ParameterType.FU16C => (BitConverter.ToUInt16(bytes) & 0x3FFF) * MathF.Pow(10, -(bytes[1] >> 6)),
                ParameterType.S32C => BitConverter.ToSingle(bytes),
                ParameterType.S32CD1 => BitConverter.ToSingle(bytes),
                ParameterType.S32CD2 => BitConverter.ToSingle(bytes),
                ParameterType.S32CD3 => BitConverter.ToSingle(bytes),
                _ => 0
            });
        }
    }
}
