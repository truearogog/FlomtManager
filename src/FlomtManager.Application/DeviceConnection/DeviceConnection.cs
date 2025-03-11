using System.Collections.Frozen;
using System.Text;
using System.Timers;
using FlomtManager.Core.Constants;
using FlomtManager.Core.DeviceConnection;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Events;
using FlomtManager.Core.Extensions;
using FlomtManager.Core.Models;
using FlomtManager.Core.Models.Collections;
using FlomtManager.Core.Parsers;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Stores;
using FlomtManager.Framework.Extensions;
using FlomtManager.Modbus;
using ParameterDictionary = System.Collections.Generic.IReadOnlyDictionary<byte, FlomtManager.Core.Models.Parameter>;
using Timer = System.Timers.Timer;

namespace FlomtManager.Application.DeviceConnection;

internal sealed class DeviceConnection(
    Device device,
    IDeviceStore deviceStore,
    IDataParser dataParser,
    IDataRepository dataRepository,
    IDeviceRepository deviceRepository,
    IParameterRepository parameterRepository) : IDeviceConnection
{
    private DeviceDefinition _deviceDefinition;
    private ParameterDictionary _currentParameters;
    private ParameterDictionary _integralParameters;

    private Timer _dataReadTimer;
    private Timer _hourArchiveReadTimer;
    private IModbusProtocol _modbusProtocol;

    public event EventHandler<DeviceConnectionStateChangedArgs> OnConnectionStateChanged;
    public event EventHandler<DeviceConnectionErrorArgs> OnConnectionError;
    public event EventHandler<DeviceConnectionDataChangedArgs> OnConnectionDataChanged;
    public event EventHandler<DeviceConnectionArchiveReadingStateChangedArgs> OnConnectionArchiveReadingStateChanged;
    public event EventHandler<DeviceConnectionArchiveReadingProgressChangedArgs> OnConnectionArchiveReadingProgressChangedArgs;

    public async ValueTask DisposeAsync()
    {
        await Disconnect();
    }

    public async Task Connect(CancellationToken cancellationToken = default)
    {
        RaiseConnectionStateChanged(ConnectionState.Connecting);
        try
        {
            _modbusProtocol = device.ConnectionType switch
            {
                ConnectionType.Serial => new ModbusProtocolSerial(
                    device.PortName ?? throw new ModbusException("Device should have Port Name."), device.BaudRate, device.Parity, device.DataBits, device.StopBits),
                ConnectionType.Network => new ModbusProtocolTcp(
                    device.IpAddress ?? throw new ModbusException("Device should have IP Address."), device.Port),
                _ => throw new NotSupportedException("Not supported device connection type.")
            };

            await _modbusProtocol.OpenAsync(cancellationToken);

            _deviceDefinition = await GetDeviceDefinition(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var existingDeviceDefinition = await deviceRepository.GetDefinitionByDeviceId(device.Id);
            if (existingDeviceDefinition == default)
            {
                await deviceRepository.CreateDefinition(_deviceDefinition);

                var parameterDefinitionBytes = await _modbusProtocol.ReadRegistersBytesAsync(device.SlaveId,
                    _deviceDefinition.ParameterDefinitionStart, DeviceConstants.MAX_PARAMETER_COUNT * 16 / 2, cancellationToken: cancellationToken);
                var parameters = dataParser.ParseParameterDefinition(parameterDefinitionBytes, device.Id);
                await parameterRepository.Create(parameters);

                deviceStore.Update(device);
            }
            else
            {
                if (existingDeviceDefinition.CRC != _deviceDefinition.CRC)
                {
                    throw new ModbusException("Device definitions have changed.");
                }
                _deviceDefinition = existingDeviceDefinition;
            }

            await dataRepository.InitDataTables(device.Id);

            cancellationToken.ThrowIfCancellationRequested();

            _currentParameters = (await parameterRepository.GetCurrentParametersByDeviceId(device.Id, true)).ToFrozenDictionary(x => x.Number);
            _integralParameters = (await parameterRepository.GetIntegralParametersByDeviceId(device.Id, true)).ToFrozenDictionary(x => x.Number);

            await ReadData();

            var dataReadSpan = 
                TimeSpan.FromSeconds(5)
                //TimeSpan.FromHours(device.ReadHours) + TimeSpan.FromMinutes(device.ReadMinutes) + TimeSpan.FromSeconds(device.ReadSeconds)
                ;
            _dataReadTimer = new Timer(dataReadSpan);
            _dataReadTimer.Elapsed += _dataReadTimer_Elapsed;
            _dataReadTimer.Start();

            await ReadHourArchive();

            _hourArchiveReadTimer = new Timer(TimeSpan.FromMinutes(10));
            _hourArchiveReadTimer.Elapsed += _hourArchiveReadTimer_Elapsed;
            _hourArchiveReadTimer.Start();

            RaiseConnectionStateChanged(ConnectionState.Connected);
        }
        catch (Exception exception)
        {
            RaiseConnectionStateChanged(ConnectionState.Disconnected);
            RaiseConnectionError(exception);
        }
    }

    private readonly SemaphoreSlim _dataReadSemaphore = new(1, 1);
    private async void _dataReadTimer_Elapsed(object sender, ElapsedEventArgs e) => await ReadData();
    private async Task ReadData()
    {
        try
        {
            await _dataReadSemaphore.WaitAsync();

            var currentParameterValues = await ReadRealtimeParameterValues(
                _deviceDefinition.CurrentParameterLineStart, _deviceDefinition.CurrentParameterLineLength, _deviceDefinition.CurrentParameterLineDefinition, _currentParameters);
            var integralParameterValues = await ReadRealtimeParameterValues(
                _deviceDefinition.IntegralParameterLineStart, _deviceDefinition.IntegralParameterLineLength, _deviceDefinition.IntegralParameterLineDefinition, _integralParameters);

            RaiseConnectionDataChanged(currentParameterValues, integralParameterValues);
        }
        catch (Exception ex)
        {
            _dataReadSemaphore.Release();
            RaiseConnectionError(ex);
        }
        finally
        {
            if (_dataReadSemaphore.CurrentCount == 0)
            {
                _dataReadSemaphore.Release();
            }
        }
    }


    private readonly SemaphoreSlim _hourArchiveReadSemaphore = new(1, 1);
    private async void _hourArchiveReadTimer_Elapsed(object sender, ElapsedEventArgs e) => await ReadHourArchive();
    private async Task ReadHourArchive()
    {
        try
        {
            await _hourArchiveReadSemaphore.WaitAsync();

            async Task<DateTime> ReadDeviceTime()
            {
                var timeBytes = await _modbusProtocol.ReadRegistersBytesAsync(device.SlaveId, 102, 3);
                return dataParser.ParseDateTime(timeBytes);
            }

            async Task<ReadOnlyMemory<byte>> ReadHourArchive(ushort lineCount) =>
                await _modbusProtocol.ReadRegistersBytesAsync(device.SlaveId, 
                    _deviceDefinition.AveragePerHourBlockStart, (ushort)(lineCount * _deviceDefinition.AverageParameterArchiveLineLength),
                    RaiseConnectionArchiveReadingProgressChangedArgs);

            RaiseConnectionArchiveReadingStateChanged(ArchiveReadingState.Reading);
            RaiseConnectionArchiveReadingProgressChangedArgs(0, 100);

            var readStartTime = await ReadDeviceTime();
            var lineCount = _deviceDefinition.AveragePerHourBlockLineCount;
            if (_deviceDefinition.LastArchiveRead is not null)
            {
                lineCount = ushort.Min(_deviceDefinition.AveragePerHourBlockLineCount, (ushort)(readStartTime - _deviceDefinition.LastArchiveRead.Value).TotalHours);
            }

            if (lineCount == 0)
            {
                RaiseConnectionArchiveReadingStateChanged(ArchiveReadingState.Complete);
                RaiseConnectionArchiveReadingProgressChangedArgs(100, 100);
                return;
            }

            var archiveBytes = await ReadHourArchive(lineCount);

            var readEndTime = await ReadDeviceTime();
            if (readStartTime.Hour != readEndTime.Hour)
            {
                lineCount = ushort.Min(_deviceDefinition.AveragePerHourBlockLineCount, (ushort)(lineCount + 1));
                readStartTime = readEndTime;
                archiveBytes = await ReadHourArchive(lineCount);
            }

            var dataCollectionFactories = new Dictionary<Type, Func<IDataCollection>>
            {
                [typeof(float)] = () => new DataCollection<float>(lineCount),
                [typeof(uint)] = () => new DataCollection<uint>(lineCount),
                [typeof(ushort)] = () => new DataCollection<ushort>(lineCount),
                [typeof(DateTime)] = () => new DataCollection<DateTime>(lineCount),
                [typeof(TimeSpan)] = () => new DataCollection<TimeSpan>(lineCount),
            };

            var parameters = (await parameterRepository.GetHourArchiveParametersByDeviceId(device.Id, true)).ToFrozenDictionary(x => x.Number);

            var parameterNumbers = new byte[parameters.Count + 1];
            parameterNumbers[0] = 0;
            parameters.Keys.CopyTo(parameterNumbers, 1);

            var data = parameterNumbers
                .Select(x => new
                {
                    Number = x,
                    ClrType = x switch
                    {
                        0 => typeof(DateTime),
                        _ => parameters[x].Type.GetClrType(),
                    }
                })
                .Select(x => new
                {
                    x.Number,
                    DataCollection = dataCollectionFactories.TryGetValue(x.ClrType, out var factory) ? factory() : null,
                })
                .Where(x => x.DataCollection is not null)
                .ToFrozenDictionary(x => x.Number, x => x.DataCollection);

            var emptyBlock = Enumerable.Repeat<byte>(0, _deviceDefinition.AverageParameterArchiveLineLength).ToArray();
            var dateHours = readStartTime.Date.AddHours(readStartTime.Hour);

            Parallel.For(0, lineCount, i =>
            {
                var date = dateHours.AddHours(-i);
                (data[0] as DataCollection<DateTime>).Values[i] = date;

                var blockBytes = archiveBytes.Slice(i * _deviceDefinition.AverageParameterArchiveLineLength, _deviceDefinition.AverageParameterArchiveLineLength);
                if (blockBytes.Span.SequenceEqual(emptyBlock))
                {
                    return;
                }

                var current = 0;
                foreach (var parameterByte in _deviceDefinition.AverageParameterArchiveLineDefinition)
                {
                    if ((parameterByte & 0b1000000) == 0)
                    {
                        var size = parameters[parameterByte].Type.GetSize();
                        var bytes = blockBytes.Slice(current, size);

                        // parse data according to type
                        if (data[parameterByte] is DataCollection<float> floatDataCollection)
                        {
                            floatDataCollection.Values[i] = dataParser.ParseBytesToFloat(bytes.Span, parameters[parameterByte]);
                        }
                        if (data[parameterByte] is DataCollection<uint> uintDataCollection)
                        {
                            uintDataCollection.Values[i] = dataParser.ParseBytesToUInt32(bytes.Span, parameters[parameterByte]);
                        }
                        if (data[parameterByte] is DataCollection<ushort> ushortDataCollection)
                        {
                            ushortDataCollection.Values[i] = dataParser.ParseBytesToUInt16(bytes.Span, parameters[parameterByte]);
                        }
                        if (data[parameterByte] is DataCollection<TimeSpan> timeSpanDataCollection)
                        {
                            timeSpanDataCollection.Values[i] = dataParser.ParseBytesToTimeSpan(bytes.Span, parameters[parameterByte]);
                        }
                        if (data[parameterByte] is DataCollection<DateTime> dateTimeDataCollection)
                        {
                            dateTimeDataCollection.Values[i] = dataParser.ParseBytesToDateTime(bytes.Span, parameters[parameterByte]);
                        }

                        current += size;
                    }
                    else
                    {
                        current += parameterByte & 0b11;
                    }
                }
            });

            await dataRepository.AddHourData(device.Id, data, lineCount);
            await deviceRepository.UpdateDefinitionLastArchiveRead(device.Id);

            RaiseConnectionArchiveReadingStateChanged(ArchiveReadingState.Complete, lineCount);
            RaiseConnectionArchiveReadingProgressChangedArgs(100, 100);
        }
        catch (Exception ex)
        {
            _hourArchiveReadSemaphore.Release();
            RaiseConnectionError(ex);
            RaiseConnectionArchiveReadingStateChanged(ArchiveReadingState.None, 0);
            RaiseConnectionArchiveReadingProgressChangedArgs(0, 100);
        }
        finally
        {
            if (_hourArchiveReadSemaphore.CurrentCount == 0)
            {
                _hourArchiveReadSemaphore.Release();
            }
        }
    }

    public async Task Disconnect(CancellationToken cancellationToken = default)
    {
        _dataReadTimer?.Dispose();
        _dataReadTimer?.Stop();
        _dataReadTimer = null;
        _hourArchiveReadTimer?.Dispose();
        _hourArchiveReadTimer?.Stop();
        _hourArchiveReadTimer = null;
        await (_modbusProtocol?.DisposeAsync() ?? ValueTask.CompletedTask);
        _modbusProtocol = null;

        RaiseConnectionStateChanged(ConnectionState.Disconnected);
        RaiseConnectionArchiveReadingStateChanged(ArchiveReadingState.None);
    }

    private async Task<DeviceDefinition> GetDeviceDefinition(CancellationToken cancellationToken = default)
    {
        var deviceDefinitionRegisters = await _modbusProtocol.ReadRegistersAsync(device.SlaveId, 116, 42, cancellationToken: cancellationToken);
        var deviceDefinition = new DeviceDefinition
        {
            DeviceId = device.Id,

            ParameterDefinitionStart = deviceDefinitionRegisters[0],
            ParameterDefinitionNumber = deviceDefinitionRegisters[1],
            DescriptionStart = deviceDefinitionRegisters[2],
            ProgramVersionStart = deviceDefinitionRegisters[3],

            CurrentParameterLineDefinitionStart = deviceDefinitionRegisters[4],
            CurrentParameterLineNumber = (byte)deviceDefinitionRegisters[5],
            CurrentParameterLineLength = (byte)(deviceDefinitionRegisters[5] >> 8),
            CurrentParameterLineStart = deviceDefinitionRegisters[6],

            IntegralParameterLineDefinitionStart = deviceDefinitionRegisters[7],
            IntegralParameterLineNumber = (byte)deviceDefinitionRegisters[8],
            IntegralParameterLineLength = (byte)(deviceDefinitionRegisters[8] >> 8),
            IntegralParameterLineStart = deviceDefinitionRegisters[9],

            AverageParameterArchiveLineDefinitionStart = deviceDefinitionRegisters[10],
            AverageParameterArchiveLineNumber = (byte)deviceDefinitionRegisters[11],
            AverageParameterArchiveLineLength = (byte)(deviceDefinitionRegisters[11] >> 8),

            AveragePerHourBlockStart = deviceDefinitionRegisters[14],
            AveragePerHourBlockLineCount = deviceDefinitionRegisters[15],
        };

        var currentParameterDefinition = await _modbusProtocol.ReadRegistersBytesAsync(device.SlaveId,
            deviceDefinition.CurrentParameterLineDefinitionStart, (ushort)(deviceDefinition.CurrentParameterLineNumber / 2), cancellationToken: cancellationToken);
        var integralParameterDefinition = await _modbusProtocol.ReadRegistersBytesAsync(device.SlaveId,
            deviceDefinition.IntegralParameterLineDefinitionStart, (ushort)(deviceDefinition.IntegralParameterLineNumber / 2), cancellationToken: cancellationToken);
        var averageParameterArchiveDefinition = await _modbusProtocol.ReadRegistersBytesAsync(device.SlaveId,
            deviceDefinition.AverageParameterArchiveLineDefinitionStart, (ushort)(deviceDefinition.AverageParameterArchiveLineNumber / 2), cancellationToken: cancellationToken);

        deviceDefinition = deviceDefinition with
        {
            CurrentParameterLineDefinition = currentParameterDefinition,
            IntegralParameterLineDefinition = integralParameterDefinition,
            AverageParameterArchiveLineDefinition = averageParameterArchiveDefinition
        };

        deviceDefinition = deviceDefinition with { CRC = CRCHelper.GetCRC(deviceDefinition.GetBytes()) };

        return deviceDefinition;
    }

    private async Task<IReadOnlyDictionary<byte, ParameterValue>> ReadRealtimeParameterValues(
        ushort lineStart, ushort lineSize, byte[] parameterDefinition, ParameterDictionary parameters, CancellationToken cancellationToken = default)
    {
        if (_modbusProtocol == null)
        {
            return new Dictionary<byte, ParameterValue>();
        }

        var parameterLine = await _modbusProtocol.ReadRegistersBytesAsync(device.SlaveId, lineStart, (ushort)(lineSize / 2), cancellationToken: cancellationToken);
        var current = 0;
        Dictionary<byte, ParameterValue> result = [];
        ushort error = 0;
        foreach (var parameterByte in parameterDefinition)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((parameterByte & 0x80) == 0)
            {
                var parameter = parameters[parameterByte];
                var bytes = parameterLine.AsSpan(current, parameter.Type.GetSize());
                var value = dataParser.ParseBytesToString(bytes, parameter);
                if (parameter.Type == ParameterType.Error)
                {
                    error = dataParser.ParseUInt16(bytes);
                }
                result[parameter.Number] = new ParameterValue
                {
                    Value = value,
                    Error = false,
                };
                current += parameter.Type.GetSize();
            }
            else
            {
                current += parameterByte & 0b111;
            }
        }

        foreach (var (parameterNumber, parameterValue) in result)
        {
            parameterValue.Error = (error & parameters[parameterNumber].ErrorMask) != 0;
        }

        return result.AsReadOnly();
    }

    private void RaiseConnectionStateChanged(ConnectionState state)
    {
        OnConnectionStateChanged?.Invoke(this, new DeviceConnectionStateChangedArgs
        {
            State = state
        });
    }

    private void RaiseConnectionDataChanged(IReadOnlyDictionary<byte, ParameterValue> currentParameterValues, IReadOnlyDictionary<byte, ParameterValue> integralParameterValues)
    {
        OnConnectionDataChanged?.Invoke(this, new DeviceConnectionDataChangedArgs
        {
            CurrentParameterValues = currentParameterValues,
            IntegralParameterValues = integralParameterValues
        });
    }

    private void RaiseConnectionError(Exception exception)
    {
        OnConnectionError?.Invoke(this, new DeviceConnectionErrorArgs
        {
            Exception = exception
        });
    }

    private void RaiseConnectionArchiveReadingStateChanged(ArchiveReadingState state, int linesRead = 0)
    {
        OnConnectionArchiveReadingStateChanged?.Invoke(this, new DeviceConnectionArchiveReadingStateChangedArgs
        {
            State = state,
            LinesRead = linesRead
        });
    }

    private void RaiseConnectionArchiveReadingProgressChangedArgs(int progress, int maxProgress)
    {
        OnConnectionArchiveReadingProgressChangedArgs?.Invoke(this, new DeviceConnectionArchiveReadingProgressChangedArgs
        {
            Progress = progress,
            MaxProgress = maxProgress
        });
    }
}
