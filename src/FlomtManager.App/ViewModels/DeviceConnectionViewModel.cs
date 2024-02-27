﻿using Avalonia.Platform.Storage;
using FlomtManager.App.Models;
using FlomtManager.App.Pages;
using FlomtManager.App.Stores;
using FlomtManager.Core.Attributes;
using FlomtManager.Core.Constants;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;
using FlomtManager.Framework.Extensions;
using FlomtManager.Modbus;
using HexIO;
using ReactiveUI;
using Serilog;
using System.Buffers.Binary;
using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace FlomtManager.App.ViewModels
{
    public class DeviceConnectionViewModel : ViewModelBase
    {
        private readonly DeviceStore _deviceStore;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IModbusService _modbusService;
        private readonly IDeviceDefinitionRepository _deviceDefinitionRepository;
        private readonly IParameterRepository _parameterRepository;

        private ConnectionState _connectionState;
        public ConnectionState ConnectionState
        {
            get => _connectionState;
            set => this.RaiseAndSetIfChanged(ref _connectionState, value);
        }

        private byte _errorNumber;

        // parameter number, parameters
        private IReadOnlyDictionary<byte, Parameter>? _parameters;

        // parameter type, size in bytes
        private IReadOnlyDictionary<ParameterType, byte> _parameterTypeSizes;

        private IModbusProtocol? _modbusProtocol;
        private Device? _device;
        private DeviceDefinition? _deviceDefinition;
        private Timer? _dataReadTimer;

        public event EventHandler<DeviceConnectionDataEventArgs>? OnConnectionData;
        public event EventHandler<DeviceConnectionErrorEventArgs>? OnConnectionError;

        public DeviceConnectionViewModel(DeviceStore deviceStore, IDeviceRepository deviceRepository, IModbusService modbusService,
            IDeviceDefinitionRepository deviceDefinitionRepository, IParameterRepository parameterRepository)
        {
            _deviceStore = deviceStore;
            _deviceRepository = deviceRepository;
            _modbusService = modbusService;
            _deviceDefinitionRepository = deviceDefinitionRepository;
            _parameterRepository = parameterRepository;

            var parameterTypeType = typeof(ParameterType);
            _parameterTypeSizes = Enum.GetValues<ParameterType>().ToFrozenDictionary(
                x => x,
                x => x.GetAttribute<SizeAttribute>()?.Size ?? throw new Exception("Wrong parameter size."));

            _dataReadTimer = new Timer(TimeSpan.FromSeconds(5));
            _dataReadTimer.Elapsed += _dataReadTimer_Elapsed;
        }

        private SemaphoreSlim _semaphore = new(1, 1);
        private async void _dataReadTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            await _semaphore.WaitAsync();

            try
            {
                ArgumentNullException.ThrowIfNull(_parameters);
                ArgumentNullException.ThrowIfNull(_deviceDefinition);
                ArgumentNullException.ThrowIfNull(_deviceDefinition.CurrentParameterLineDefinition);
                ArgumentNullException.ThrowIfNull(_deviceDefinition.IntegralParameterLineDefinition);

                // read parameter values
                var currentParameters = await ReadParameterLine(_deviceDefinition.CurrentParameterLineStart, _deviceDefinition.CurrentParameterLineLength, 
                    _deviceDefinition.CurrentParameterLineDefinition);
                var integralParameters = await ReadParameterLine(_deviceDefinition.IntegralParameterLineStart, _deviceDefinition.CurrentParameterLineLength,
                    _deviceDefinition.IntegralParameterLineDefinition);

                // get error parameter value and apply it to parameters
                var error = ushort.Parse(currentParameters[_errorNumber].Value);
                foreach (var parameterNumber in currentParameters.Keys)
                {
                    currentParameters[parameterNumber].Error = (_parameters[parameterNumber].ErrorMask & error) > 0;
                }

                OnConnectionData?.Invoke(this, new()
                {
                    CurrentParameters = currentParameters,
                    IntegralParameters = integralParameters
                });
            }
            catch (Exception ex)
            {
                OnConnectionError?.Invoke(this, new()
                {
                    Exception = ex
                });
                await Disconnect();
                Log.Error(ex, string.Empty);
            }

            _semaphore.Release();
        }

        public async Task Connect(Device device, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_dataReadTimer);

                ConnectionState = ConnectionState.Connecting;

                // create modbus protocol
                _modbusProtocol = device.ConnectionType switch
                {
                    ConnectionType.Serial => new ModbusProtocolSerial(
                        device.PortName ?? throw new ModbusException("Device should have Port Name."), device.BaudRate, device.Parity, device.DataBits, device.StopBits),
                    ConnectionType.Network => new ModbusProtocolTcp(
                        device.IpAddress ?? throw new ModbusException("Device should have IP Address."), device.Port),
                    _ => throw new NotSupportedException("Not supported device connection type.")
                };

                await _modbusProtocol.OpenAsync(cancellationToken);
                var deviceDefinition = await _modbusService.ReadDeviceDefinition(_modbusProtocol, device.SlaveId, cancellationToken);

                var currentParameterDefinition = await _modbusProtocol.ReadRegistersBytesAsync(
                    device.SlaveId, deviceDefinition.CurrentParameterLineDefinitionStart, (ushort)(deviceDefinition.CurrentParameterLineNumber / 2), cancellationToken: cancellationToken);
                deviceDefinition.CurrentParameterLineDefinition = currentParameterDefinition;

                var integralParameterDefinition = await _modbusProtocol.ReadRegistersBytesAsync(
                    device.SlaveId, deviceDefinition.IntegralParameterLineDefinitionStart, (ushort)(deviceDefinition.IntegralParameterLineNumber / 2), cancellationToken: cancellationToken);
                deviceDefinition.IntegralParameterLineDefinition = integralParameterDefinition;

                var averageParameterArchiveDefinition = await _modbusProtocol.ReadRegistersBytesAsync(
                    device.SlaveId, deviceDefinition.AverageParameterArchiveLineDefinitionStart, (ushort)(deviceDefinition.AverageParameterArchiveLineNumber / 2), cancellationToken: cancellationToken);
                deviceDefinition.AverageParameterArchiveLineDefinition = averageParameterArchiveDefinition;

                deviceDefinition.DeviceId = device.Id;
                cancellationToken.ThrowIfCancellationRequested();

                // if first time - read device definition and parameter definitions
                if (device.DeviceDefinitionId == 0)
                {
                    var deviceDefinitionId = await _deviceDefinitionRepository.Create(deviceDefinition);
                    device.DeviceDefinitionId = deviceDefinitionId;
                    await _deviceStore.UpdateDevice(_deviceRepository, device);

                    var parameters = await _modbusService.ReadParameterDefinitions(_modbusProtocol, device.SlaveId, deviceDefinition, cancellationToken);
                    foreach (var parameter in parameters)
                    {
                        parameter.DeviceId = device.Id;
                    }
                    await _parameterRepository.CreateRange(parameters);
                }

                // else - read device definition and compare crc
                else
                {
                    var oldDeviceDefinition = await _deviceDefinitionRepository.GetById(device.DeviceDefinitionId);
                    if (deviceDefinition.CRC != oldDeviceDefinition!.CRC)
                    {
                        throw new ModbusException("Device definitions have changed.");
                    }
                }

                _device = device;
                _deviceDefinition = deviceDefinition;

                cancellationToken.ThrowIfCancellationRequested();
                _parameters = (await _parameterRepository.GetAll(x => x.DeviceId == device.Id)).ToFrozenDictionary(x => x.Number, x => x);
                _errorNumber = _parameters.Values.FirstOrDefault(x => x.ParameterType == ParameterType.Error)?.Number ?? 0;

                _dataReadTimer.Start();

                ConnectionState = ConnectionState.Connected;
            }
            catch (Exception ex)
            {
                await Disconnect(cancellationToken);
                Log.Error(ex, string.Empty);
                throw;
            }
        }

        public async Task Disconnect(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(_dataReadTimer);
            if (_modbusProtocol?.IsOpen == true)
            {
                await _modbusProtocol.CloseAsync(cancellationToken);
            }
            _dataReadTimer.Stop();
            ConnectionState = ConnectionState.Disconnected;
        }

        public async Task ReadArchivesFromDevice(CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_device);
                ArgumentNullException.ThrowIfNull(_modbusProtocol);

                var bytes = await _modbusProtocol.ReadRegistersBytesAsync(_device.SlaveId, 0, DeviceConstants.MEMORY_SIZE_REGISTERS, cancellationToken: cancellationToken);

                await ParseDeviceMemory(_device, bytes.AsMemory(), cancellationToken);
            }
            catch (Exception ex)
            {
                OnConnectionError?.Invoke(this, new()
                {
                    Exception = ex
                });
                await Disconnect(cancellationToken);
                Log.Error(ex, string.Empty);
            }
        }

        public async Task ReadArchivesFromFile(Device device, IStorageFile file, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var stream = await file.OpenReadAsync();
                using var reader = new IntelHexStreamReader(stream);
                var bytes = new byte[DeviceConstants.MEMORY_SIZE_BYTES];
                var total = 0;
                while (!reader.State.Eof)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var record = reader.ReadHexRecord();
                    record.Data.CopyTo(bytes, total);
                    total += record.RecordLength;
                }

                cancellationToken.ThrowIfCancellationRequested();
                await ParseDeviceMemory(device, bytes.AsMemory(), cancellationToken);
            }
            catch (Exception ex)
            {
                OnConnectionError?.Invoke(this, new()
                {
                    Exception = ex
                });
                Log.Error(ex, string.Empty);
            }
        }

        private async Task ParseDeviceMemory(Device device, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            var definitionBytes = bytes.Slice(116, 84);
            var registerCount = definitionBytes.Length / 2;
            var registers = new ushort[registerCount];
            for (int i = 0; i < registerCount; i++)
            {
                registers[i] = definitionBytes.Span[2 * i + 1];
                registers[i] <<= 8;
                registers[i] += definitionBytes.Span[2 * i];
            }

            var deviceDefinition = _modbusService.ParseDeviceDefinition(registers);
            deviceDefinition.DeviceId = device.Id;

            if (device.DeviceDefinitionId == 0)
            {
                var deviceDefinitionId = await _deviceDefinitionRepository.Create(deviceDefinition);
                device.DeviceDefinitionId = deviceDefinitionId;
                await _deviceStore.UpdateDevice(_deviceRepository, device);

                var parameterBytes = bytes.Slice(deviceDefinition.ParameterDefinitionStart, DeviceConstants.MAX_PARAMETER_COUNT * 16);
                var parameters = _modbusService.ParseParameterDefinitions(parameterBytes.Span);
                foreach (var parameter in parameters)
                {
                    parameter.DeviceId = device.Id;
                }
                await _parameterRepository.CreateRange(parameters);
            }

            // else - read device definition and compare crc
            else
            {
                var oldDeviceDefinition = await _deviceDefinitionRepository.GetById(device.DeviceDefinitionId);
                if (deviceDefinition.CRC != oldDeviceDefinition!.CRC)
                {
                    throw new ModbusException("Device definitions have changed.");
                }
            }


        }

        private async Task<IReadOnlyDictionary<byte, ParameterValue>> ReadParameterLine(
            ushort lineStart, ushort lineSize, byte[] parameterDefinition, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(_device);
            ArgumentNullException.ThrowIfNull(_parameters);
            ArgumentNullException.ThrowIfNull(_modbusProtocol);

            var parameterLine = await _modbusProtocol.ReadRegistersBytesAsync(_device.SlaveId, lineStart, (ushort)(lineSize / 2), cancellationToken: cancellationToken);
            var current = 0;
            Dictionary<byte, ParameterValue> result = [];
            foreach (var parameterByte in parameterDefinition)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if ((parameterByte & 0x80) == 0)
                {
                    var parameter = _parameters[parameterByte];
                    var size = _parameterTypeSizes[parameter.ParameterType];
                    var value = ParseBytesToValue(parameterLine.AsSpan(current, size), parameter.ParameterType, parameter.Comma);
                    result.Add(parameter.Number, new() { Value = value });
                    current += size;
                }
                else
                {
                    current += parameterByte & 0xF;
                }
            }

            return result.AsReadOnly();
        }

        private string ParseBytesToValue(ReadOnlySpan<byte> bytes, ParameterType type, byte comma)
        {
            return type switch
            {
                ParameterType.S16C => StringParseS16C(bytes, comma),
                ParameterType.U16C => StringParseU16C(bytes, comma),
                ParameterType.FS16C => StringParseFS16C(bytes, comma),
                ParameterType.FU16C => StringParseFU16C(bytes, comma),
                ParameterType.S32C => StringParseS32C(bytes, comma, 0),
                ParameterType.S32CD1 => StringParseS32C(bytes, comma, 1),
                ParameterType.S32CD2 => StringParseS32C(bytes, comma, 2),
                ParameterType.S32CD3 => StringParseS32C(bytes, comma, 3),
                ParameterType.Error => BitConverter.ToUInt16(bytes).ToString(),
                ParameterType.WorkingTimeInSeconds => SecondsToString(BinaryPrimitives.ReadUInt32LittleEndian(bytes)),
                ParameterType.WorkingTimeInSecondsInArchiveInterval => BitConverter.ToUInt16(bytes).ToString(),
                ParameterType.WorkingTimeInMinutesInArchiveInterval => BitConverter.ToUInt16(bytes).ToString(),
                ParameterType.WorkingTimeInHoursInArchiveInterval => BitConverter.ToUInt16(bytes).ToString(),
                ParameterType.Time => string.Join("", bytes.ToArray()),
                ParameterType.SecondsSince2000 => BitConverter.ToUInt32(bytes).ToString(),
                _ => string.Empty
            };
        }

        private string FloatToString(float value, byte comma)
        {
            return comma switch
            {
                0 => value.ToString(),
                >= 1 and <= 4 => value.ToString("F" + comma),
                7 => value.ToString(),
                _ => throw new NotSupportedException()
            };
        }

        private float ParseS16C(ReadOnlySpan<byte> bytes, byte comma)
        {
            var value = BinaryPrimitives.ReadInt16LittleEndian(bytes);
            var commaMultiplier = _modbusService.GetComma(comma);
            return value * commaMultiplier;
        }

        private string StringParseS16C(ReadOnlySpan<byte> bytes, byte comma)
        {
            return FloatToString(ParseS16C(bytes, comma), comma);
        }

        private float ParseU16C(ReadOnlySpan<byte> bytes, byte comma)
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
            var commaMultiplier = _modbusService.GetComma(comma);
            return value * commaMultiplier;
        }

        private string StringParseU16C(ReadOnlySpan<byte> bytes, byte comma)
        {
            return FloatToString(ParseU16C(bytes, comma), comma);
        }

        private float ParseFS16C(ReadOnlySpan<byte> bytes, byte comma)
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
            var mantissa = value & 0x3FFF;
            var sign = ((value >> 14) & 1) == 0 ? 1 : -1;
            var exponent = -(value >> 15);
            var commaMultiplier = _modbusService.GetComma(comma);
            return mantissa * sign * MathF.Pow(10, exponent) * commaMultiplier;
        }

        private string StringParseFS16C(ReadOnlySpan<byte> bytes, byte comma)
        {
            return FloatToString(ParseFS16C(bytes, comma), comma);
        }

        private float ParseFU16C(ReadOnlySpan<byte> bytes, byte comma)
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
            var mantissa = value & 0x3FFF;
            var exponent = value >> 14;
            var commaMultiplier = _modbusService.GetComma(comma);
            return mantissa * MathF.Pow(10, exponent) * commaMultiplier;
        }

        private string StringParseFU16C(ReadOnlySpan<byte> bytes, byte comma)
        {
            return FloatToString(ParseFU16C(bytes, comma), comma);
        }

        private float ParseS32C(ReadOnlySpan<byte> bytes, byte comma, byte trim)
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(bytes);
            var commaMultiplier = _modbusService.GetComma(comma);
            return (value * commaMultiplier).TrimDecimalPlaces(trim);
        }

        private string StringParseS32C(ReadOnlySpan<byte> bytes, byte comma, byte trim)
        {
            return FloatToString(ParseS32C(bytes, comma, trim), comma);
        }

        private static string SecondsToString(uint seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            // Using the custom format "hhhh:mm:ss" for hours, minutes, and seconds
            return $"{timeSpan.Days * 24 + timeSpan.Hours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }
}
