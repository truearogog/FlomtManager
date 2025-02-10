using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Timers;
using Avalonia.Platform.Storage;
using FlomtManager.App.Models;
using FlomtManager.App.Stores;
using FlomtManager.Core.Attributes;
using FlomtManager.Core.Constants;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;
using FlomtManager.Framework.Extensions;
using FlomtManager.Modbus;
using HexIO;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Serilog;
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
        private readonly IDataGroupRepository _dataGroupRepository;

        private ConnectionState _connectionState;
        public ConnectionState ConnectionState
        {
            get => _connectionState;
            set => this.RaiseAndSetIfChanged(ref _connectionState, value);
        }

        private byte _errorNumber;

        // parameter number, parameters
        private IReadOnlyDictionary<byte, Parameter> _parameters;

        // parameter type, size in bytes
        private IReadOnlyDictionary<ParameterType, byte> _parameterTypeSizes;

        private Device _device;
        private DeviceDefinition _deviceDefinition;
        private IModbusProtocol _modbusProtocol;
        private readonly Timer _dataReadTimer;

        public event EventHandler<DeviceConnectionDataEventArgs> OnConnectionData;
        public event EventHandler<DeviceConnectionErrorEventArgs> OnConnectionError;

        public DeviceConnectionViewModel(
            DeviceStore deviceStore,
            IDeviceRepository deviceRepository,
            IModbusService modbusService,
            IDeviceDefinitionRepository deviceDefinitionRepository,
            IParameterRepository parameterRepository,
            IDataGroupRepository dataGroupRepository)
        {
            _deviceStore = deviceStore;
            _deviceRepository = deviceRepository;
            _modbusService = modbusService;
            _deviceDefinitionRepository = deviceDefinitionRepository;
            _parameterRepository = parameterRepository;
            _dataGroupRepository = dataGroupRepository;

            var parameterTypeType = typeof(ParameterType);
            _parameterTypeSizes = Enum.GetValues<ParameterType>()
                .ToFrozenDictionary(x => x, x => x.GetAttribute<SizeAttribute>()?.Size ?? throw new Exception("Wrong parameter size."));

            _dataReadTimer = new Timer(TimeSpan.FromSeconds(5));
            _dataReadTimer.Elapsed += _dataReadTimer_Elapsed;
        }

        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private async void _dataReadTimer_Elapsed(object sender, ElapsedEventArgs e)
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
                var integralParameters = await ReadParameterLine(_deviceDefinition.IntegralParameterLineStart, _deviceDefinition.IntegralParameterLineLength,
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
                deviceDefinition.Id = device.Id;

                cancellationToken.ThrowIfCancellationRequested();

                // if first time - read device definition and parameter definitions
                if (device.DeviceDefinitionId == 0)
                {
                    _deviceDefinitionRepository.Add(deviceDefinition);
                    await _deviceDefinitionRepository.SaveChangesAsync(cancellationToken);
                    device.DeviceDefinitionId = deviceDefinition.Id;
                    await _deviceRepository.SaveChangesAsync(cancellationToken);

                    _deviceStore.UpdateDevice(await _deviceRepository.GetByIdAsyncNonTracking(device.Id, cancellationToken));

                    var parameters = await _modbusService.ReadParameterDefinitions(_modbusProtocol, device.SlaveId, deviceDefinition, cancellationToken);
                    foreach (var parameter in parameters)
                    {
                        parameter.DeviceId = device.Id;
                    }
                    _parameterRepository.AddRange(parameters);
                    await _parameterRepository.SaveChangesAsync(cancellationToken);
                    _deviceDefinition = deviceDefinition;
                }

                // else - read device definition and compare crc
                else
                {
                    var oldDeviceDefinition = await _deviceDefinitionRepository.GetByIdAsync(device.DeviceDefinitionId, cancellationToken);
                    if (deviceDefinition.CRC != oldDeviceDefinition!.CRC)
                    {
                        throw new ModbusException("Device definitions have changed.");
                    }
                    _deviceDefinition = oldDeviceDefinition;
                }

                _device = device;

                cancellationToken.ThrowIfCancellationRequested();
                _parameters = (await _parameterRepository.GetAll().Where(x => x.DeviceId == device.Id).ToListAsync()).ToFrozenDictionary(x => x.Number, x => x);
                _errorNumber = _parameters.Values.FirstOrDefault(x => x.ParameterType == ParameterType.Error)?.Number ?? 0;

                _dataReadTimer.Start();

                ConnectionState = ConnectionState.Connected;
            }
            catch (Exception ex)
            {
                await Disconnect();
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
                ArgumentNullException.ThrowIfNull(_deviceDefinition);
                ArgumentNullException.ThrowIfNull(_modbusProtocol);

                // read average per hour archive
                var readStartTime = _modbusService.ParseDateTime(await _modbusProtocol.ReadRegistersBytesAsync(_device.SlaveId, 102, 3, cancellationToken: cancellationToken));
                var lineCount = _deviceDefinition.AveragePerHourBlockLineCount;
                if (_deviceDefinition.LastArchiveRead != null)
                {
                    lineCount = ushort.Min(_deviceDefinition.AveragePerHourBlockLineCount, (ushort)(readStartTime - _deviceDefinition.LastArchiveRead.Value).TotalHours);
                }
                var bytes = await _modbusProtocol.ReadRegistersBytesAsync(_device.SlaveId, _deviceDefinition.AveragePerHourBlockStart,
                    (ushort)(lineCount * _deviceDefinition.AverageParameterArchiveLineLength), cancellationToken: cancellationToken);
                var readEndTime = _modbusService.ParseDateTime(await _modbusProtocol.ReadRegistersBytesAsync(_device.SlaveId, 102, 3, cancellationToken: cancellationToken));
                if (readStartTime.Hour != readEndTime.Hour)
                {
                    lineCount = ushort.Min(_deviceDefinition.AveragePerHourBlockLineCount, (ushort)(lineCount + 1));
                    readStartTime = readEndTime;
                    bytes = await _modbusProtocol.ReadRegistersBytesAsync(_device.SlaveId, _deviceDefinition.AveragePerHourBlockStart,
                        (ushort)(lineCount * _deviceDefinition.AverageParameterArchiveLineLength), cancellationToken: cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                await ParseAveragePerHourArchive(readStartTime, lineCount, bytes, cancellationToken);
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

                // get device definition
                var definitionBytes = bytes.AsMemory(116, 84);
                var registerCount = definitionBytes.Length / 2;
                var registers = new ushort[registerCount];
                for (int i = 0; i < registerCount; i++)
                {
                    registers[i] = definitionBytes.Span[2 * i + 1];
                    registers[i] <<= 8;
                    registers[i] += definitionBytes.Span[2 * i];
                }

                var deviceDefinition = _modbusService.ParseDeviceDefinition(registers);
                deviceDefinition.CurrentParameterLineDefinition 
                    = bytes[deviceDefinition.CurrentParameterLineDefinitionStart.. (deviceDefinition.CurrentParameterLineDefinitionStart + deviceDefinition.CurrentParameterLineNumber)];
                deviceDefinition.IntegralParameterLineDefinition 
                    = bytes[deviceDefinition.IntegralParameterLineDefinitionStart.. (deviceDefinition.IntegralParameterLineDefinitionStart + deviceDefinition.IntegralParameterLineNumber)];
                deviceDefinition.AverageParameterArchiveLineDefinition
                    = bytes[deviceDefinition.AverageParameterArchiveLineDefinitionStart.. (deviceDefinition.AverageParameterArchiveLineDefinitionStart + deviceDefinition.AverageParameterArchiveLineNumber)];
                deviceDefinition.Id = device.Id;

                cancellationToken.ThrowIfCancellationRequested();

                if (device.DeviceDefinitionId == 0)
                {
                    _deviceDefinitionRepository.Add(deviceDefinition);
                    await _deviceDefinitionRepository.SaveChangesAsync(cancellationToken);
                    device.DeviceDefinitionId = deviceDefinition.Id;
                    await _deviceRepository.SaveChangesAsync();
                    _deviceStore.UpdateDevice(await _deviceRepository.GetByIdAsyncNonTracking(device.Id, cancellationToken));

                    var parameterBytes = bytes.AsMemory(deviceDefinition.ParameterDefinitionStart, DeviceConstants.MAX_PARAMETER_COUNT * 16);
                    var parameters = _modbusService.ParseParameterDefinitions(parameterBytes.Span);
                    foreach (var parameter in parameters)
                    {
                        parameter.DeviceId = device.Id;
                    }
                    _parameterRepository.AddRange(parameters);
                    await _parameterRepository.SaveChangesAsync(cancellationToken);
                    _deviceDefinition = deviceDefinition;
                }

                // else - read device definition and compare crc
                else
                {
                    var oldDeviceDefinition = await _deviceDefinitionRepository.GetByIdAsync(device.DeviceDefinitionId);
                    if (deviceDefinition.CRC != oldDeviceDefinition!.CRC)
                    {
                        throw new ModbusException("Device definitions have changed.");
                    }
                    _deviceDefinition = oldDeviceDefinition;
                }
                _device = device;

                var readStartTime = _modbusService.ParseDateTime(bytes.AsSpan(102, 6));

                // read average per hour archive
                var lineCount = _deviceDefinition.AveragePerHourBlockLineCount;
                if (_deviceDefinition.LastArchiveRead != null)
                {
                    lineCount = ushort.Min(_deviceDefinition.AveragePerHourBlockLineCount, (ushort)(readStartTime - _deviceDefinition.LastArchiveRead.Value).TotalHours);
                }

                var averagePerHourArchiveBytes 
                    = bytes.AsMemory(_deviceDefinition.AveragePerHourBlockStart, _deviceDefinition.AveragePerHourBlockLineCount * _deviceDefinition.AverageParameterArchiveLineLength);
                await ParseAveragePerHourArchive(readStartTime, lineCount, averagePerHourArchiveBytes, cancellationToken);
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

        private async Task ParseAveragePerHourArchive(DateTime dateTime, int lineCount, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(_device);
            ArgumentNullException.ThrowIfNull(_deviceDefinition);

            _parameters = (await _parameterRepository.GetAll().Where(x => x.DeviceId == _device.Id).ToListAsync()).ToFrozenDictionary(x => x.Number, x => x);

            var dataGroups = new List<DataGroup>();
            var emptyBlock = Enumerable.Repeat<byte>(0, _deviceDefinition.AverageParameterArchiveLineLength).ToArray();
            var dateHours = dateTime.Date.AddHours(dateTime.Hour);
            for (var i = 0; i < lineCount; i++)
            {
                var date = dateHours.AddHours(-i);
                var blockBytes = bytes.Slice(i * _deviceDefinition.AverageParameterArchiveLineLength, _deviceDefinition.AverageParameterArchiveLineLength);
                if (!blockBytes.Span.SequenceEqual(emptyBlock) && 
                    !await _dataGroupRepository.GetAll().AnyAsync(x => x.DateTime == date && x.DeviceId == _device.Id, cancellationToken))
                {
                    dataGroups.Add(new()
                    {
                        DateTime = date,
                        Data = blockBytes.ToArray(),
                        DeviceId = _device.Id,
                    });
                }
            }

            _dataGroupRepository.AddRange(dataGroups);
            await _dataGroupRepository.SaveChangesAsync(cancellationToken);

            _deviceDefinition.LastArchiveRead = DateTime.Now;
            await _deviceDefinitionRepository.SaveChangesAsync(cancellationToken);
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
                    //if (parameter.Number == 5)
                    //{
                    //    var a = BinaryPrimitives.ReadUInt16LittleEndian(parameterLine.AsSpan(current, size));
                    //    var mantissa = a & 0x3FFF;
                    //    var sign = ((a >> 14) & 1) == 0 ? 1 : -1;
                    //    var exponent = a >> 15;
                    //    var commaMultiplier = _modbusService.GetComma(parameter.Comma);
                    //    var b = mantissa * sign * MathF.Pow(10, exponent) * commaMultiplier;
                    //    Debug.WriteLine("{0} {1} {2} {3} {4}", mantissa, sign, exponent, commaMultiplier, b);
                    //}
                    var value = _modbusService.StringParseBytesToValue(parameterLine.AsSpan(current, size), parameter.ParameterType, parameter.Comma);
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
    }
}
