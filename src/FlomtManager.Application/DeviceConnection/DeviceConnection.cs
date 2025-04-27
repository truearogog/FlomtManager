using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using System.Timers;
using System.Xml.Linq;
using FlomtManager.Domain.Abstractions.DeviceConnection;
using FlomtManager.Domain.Abstractions.DeviceConnection.Events;
using FlomtManager.Domain.Abstractions.Parsers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Constants;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Extensions;
using FlomtManager.Domain.Models;
using FlomtManager.Domain.Models.Collections;
using FlomtManager.Framework.Extensions;
using FlomtManager.Framework.Skia.ColorPalletes;
using FlomtManager.Modbus;
using SkiaSharp;
using ParameterDictionary = System.Collections.Generic.IReadOnlyDictionary<byte, FlomtManager.Domain.Models.Parameter>;
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
                    _deviceDefinition.ParameterDefinitionStart, DeviceConstants.MAX_PARAMETER_COUNT * DeviceConstants.PARAMETER_SIZE / 2, cancellationToken: cancellationToken);

                var previousDevice = await deviceRepository.GetLastCreatedExcept(device.Id);
                var previousParameterColors = (await parameterRepository.GetAllByDeviceId(previousDevice.Id)).ToImmutableDictionary(x => x.Name, x => x.Color);

                var palette = new VibrantSKColorPalette();

                var knownParameterNameColors = new Dictionary<string, SKColor>
                {
                    ["P"] = palette.GetColor(0, 3),
                    ["G"] = palette.GetColor(1, 3),
                    ["Q"] = palette.GetColor(4, 3),
                    ["t"] = palette.GetColor(7, 3),
                    ["tc"] = palette.GetColor(3, 3),
                    ["p"] = palette.GetColor(5, 3),
                };

                var knownParameterTypeColors = new Dictionary<ParameterType, SKColor>
                {
                    [ParameterType.WorkingTimeInSecondsInArchiveInterval] = palette.GetColor(8, 3),
                };

                var parameters = dataParser.ParseParameterDefinition(parameterDefinitionBytes, device.Id);
                parameters = parameters
                    .Select(x => {
                        var color = palette.GetColor(8, 3);
                        if (previousParameterColors.TryGetValue(x.Name, out var prevColor))
                        {
                            color = SKColor.Parse(prevColor);
                        }
                        else if (knownParameterNameColors.TryGetValue(x.Name, out var nameColor))
                        {
                            color = nameColor;
                        }
                        else if (knownParameterTypeColors.TryGetValue(x.Type, out var typeColor))
                        {
                            color = typeColor;
                        }

                        return x with { Color = color.ToString() };
                    })
                    .ToList().AsReadOnly();
                await parameterRepository.Create(parameters);

                deviceStore.Update(device);
            }
            else
            {
                if (existingDeviceDefinition.CRC != _deviceDefinition.CRC)
                {
                    throw new Exception("Device definitions have changed.");
                }
                _deviceDefinition = existingDeviceDefinition;
            }

            await dataRepository.InitDataTables(device.Id);

            cancellationToken.ThrowIfCancellationRequested();

            _currentParameters = (await parameterRepository.GetCurrentParametersByDeviceId(device.Id, true)).ToFrozenDictionary(x => x.Number);
            _integralParameters = (await parameterRepository.GetIntegralParametersByDeviceId(device.Id, true)).ToFrozenDictionary(x => x.Number);

            await ReadData();
            await ReadHourArchive();

            RaiseConnectionStateChanged(ConnectionState.Connected);

            if (device.DataReadEnabled)
            {
                _dataReadTimer = new Timer(TimeSpan.FromTicks(device.DataReadIntervalTicks));
                _dataReadTimer.Elapsed += _dataReadTimer_Elapsed;
                _dataReadTimer.Start();

                _hourArchiveReadTimer = new Timer(TimeSpan.FromMinutes(10));
                _hourArchiveReadTimer.Elapsed += _hourArchiveReadTimer_Elapsed;
                _hourArchiveReadTimer.Start();
            }
            else
            {
                await Disconnect(cancellationToken);
                RaiseConnectionStateChanged(ConnectionState.Disconnected);
            }
        }
        catch (Exception ex)
        {
            RaiseConnectionStateChanged(ConnectionState.Disconnected);
            RaiseConnectionError(ex);
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
        async Task CompleteRead(int lineCount)
        {
            RaiseConnectionArchiveReadingProgressChangedArgs(100, 100);
            RaiseConnectionArchiveReadingStateChanged(ArchiveReadingState.Complete, lineCount);

            await Task.Delay(TimeSpan.FromSeconds(1));
            RaiseConnectionArchiveReadingStateChanged(ArchiveReadingState.None, lineCount);
        }

        try
        {
            await _hourArchiveReadSemaphore.WaitAsync();

            async Task<DateTime> ReadDeviceTime()
            {
                var timeBytes = await _modbusProtocol.ReadRegistersBytesAsync(device.SlaveId, DeviceConstants.CURRENT_DATETIME_START, DeviceConstants.CURRENT_DATETIME_SIZE_REGISTERS);
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
                if (readStartTime.Hour == _deviceDefinition.LastArchiveRead.Value.Hour)
                {
                    lineCount = 0;
                }
                else
                {
                    var timeDiff = readStartTime - _deviceDefinition.LastArchiveRead.Value;
                    // border case
                    if (timeDiff.Hours < 1 && readStartTime.Hour != _deviceDefinition.LastArchiveRead.Value.Hour)
                    {
                        lineCount = 1;
                    }
                    else
                    {
                        lineCount = (ushort)int.Clamp((int)timeDiff.TotalHours, 0, _deviceDefinition.AveragePerHourBlockLineCount);
                    }
                }
            }

            if (lineCount == 0)
            {
                await CompleteRead(0);
                return;
            }

            var archiveBytes = await ReadHourArchive(lineCount);

            var readEndTime = await ReadDeviceTime();
            if (readStartTime.Hour != readEndTime.Hour)
            {
                lineCount = ushort.Min(_deviceDefinition.AveragePerHourBlockLineCount, (ushort)(lineCount + 1));
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

            var dateHours = readEndTime.Date.AddHours(readEndTime.Hour);
            var actualLineCount = 0;
            for (var i = 0; i < lineCount; ++i)
            {
                var date = dateHours.AddHours(-i);
                var index = lineCount - actualLineCount - 1;
                (data[0] as DataCollection<DateTime>).Values[index] = date;

                var blockBytes = archiveBytes.Slice(i * _deviceDefinition.AverageParameterArchiveLineLength, _deviceDefinition.AverageParameterArchiveLineLength);
                var current = 0;
                foreach (var parameterByte in _deviceDefinition.AverageParameterArchiveLineDefinition)
                {
                    if ((parameterByte & 0b10000000) == 0)
                    {
                        var size = parameters[parameterByte].Type.GetSize();
                        var valueBytes = blockBytes.Slice(current, size);

                        // parse data according to type
                        if (data[parameterByte] is DataCollection<float> floatDataCollection)
                        {
                            floatDataCollection.Values[index] = dataParser.ParseBytesToFloat(valueBytes.Span, parameters[parameterByte]);
                        } 
                        else if (data[parameterByte] is DataCollection<uint> uintDataCollection)
                        {
                            uintDataCollection.Values[index] = dataParser.ParseBytesToUInt32(valueBytes.Span, parameters[parameterByte]);
                        }
                        else if (data[parameterByte] is DataCollection<ushort> ushortDataCollection)
                        {
                            ushortDataCollection.Values[index] = dataParser.ParseBytesToUInt16(valueBytes.Span, parameters[parameterByte]);
                        }
                        else if (data[parameterByte] is DataCollection<TimeSpan> timeSpanDataCollection)
                        {
                            timeSpanDataCollection.Values[index] = dataParser.ParseBytesToTimeSpan(valueBytes.Span, parameters[parameterByte]);
                        }
                        else if (data[parameterByte] is DataCollection<DateTime> dateTimeDataCollection)
                        {
                            dateTimeDataCollection.Values[index] = dataParser.ParseBytesToDateTime(valueBytes.Span, parameters[parameterByte]);
                        }

                        current += size;
                    }
                    else
                    {
                        current += parameterByte & 0b11;
                    }
                }
                ++actualLineCount;
            }

            await dataRepository.AddHourData(device.Id, data, lineCount, actualLineCount);
            await deviceRepository.UpdateDefinitionLastArchiveRead(device.Id, readEndTime);

            await CompleteRead(actualLineCount);
        }
        catch (Exception ex)
        {
            _hourArchiveReadSemaphore.Release();
            RaiseConnectionError(ex);
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
        var deviceDefinitionRegisters = await _modbusProtocol.ReadRegistersAsync(device.SlaveId, 
            DeviceConstants.MEMORY_DEFINITION_START, DeviceConstants.MEMORY_DEFINITION_LENGTH_REGISTERS, cancellationToken: cancellationToken);
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
            if ((parameterByte & 0b10000000) == 0)
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
