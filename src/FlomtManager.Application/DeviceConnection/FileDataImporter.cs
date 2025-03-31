using System.Collections.Frozen;
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
using FlomtManager.Modbus;
using HexIO;
using ParameterDictionary = System.Collections.Generic.IReadOnlyDictionary<byte, FlomtManager.Domain.Models.Parameter>;

namespace FlomtManager.Application.DeviceConnection;

internal sealed class FileDataImporter(
    Device device,
    IDeviceStore deviceStore,
    IDataParser dataParser,
    IDataRepository dataRepository,
    IDeviceRepository deviceRepository,
    IParameterRepository parameterRepository) : IFileDataImporter
{
    private ParameterDictionary _currentParameters;
    private ParameterDictionary _integralParameters;

    public event EventHandler<DeviceConnectionStateChangedArgs> OnConnectionStateChanged;
    public event EventHandler<DeviceConnectionErrorArgs> OnConnectionError;
    public event EventHandler<DeviceConnectionDataChangedArgs> OnConnectionDataChanged;
    public event EventHandler<DeviceConnectionArchiveReadingStateChangedArgs> OnConnectionArchiveReadingStateChanged;
    public event EventHandler<DeviceConnectionArchiveReadingProgressChangedArgs> OnConnectionArchiveReadingProgressChangedArgs;

    public async Task Import(Stream stream, CancellationToken cancellationToken = default)
    {
        RaiseConnectionStateChanged(ConnectionState.ReadingFile);
        try
        {
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

            var definitionBytes = bytes.AsMemory(DeviceConstants.MEMORY_DEFINITION_START, DeviceConstants.MEMORY_DEFINITION_LENGTH_BYTES);
            var deviceDefinitionRegisterCount = definitionBytes.Length / 2;
            var deviceDefinitionRegisters = new ushort[deviceDefinitionRegisterCount];
            for (int i = 0; i < deviceDefinitionRegisterCount; i++)
            {
                deviceDefinitionRegisters[i] = definitionBytes.Span[2 * i + 1];
                deviceDefinitionRegisters[i] <<= 8;
                deviceDefinitionRegisters[i] += definitionBytes.Span[2 * i];
            }

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

            var currentParameterDefinition = bytes.AsSpan(deviceDefinition.CurrentParameterLineDefinitionStart, deviceDefinition.CurrentParameterLineNumber);
            var integralParameterDefinition = bytes.AsSpan(deviceDefinition.IntegralParameterLineDefinitionStart, deviceDefinition.IntegralParameterLineNumber);
            var averageParameterArchiveDefinition = bytes.AsSpan(deviceDefinition.AverageParameterArchiveLineDefinitionStart, deviceDefinition.AverageParameterArchiveLineNumber);

            deviceDefinition = deviceDefinition with
            {
                CurrentParameterLineDefinition = currentParameterDefinition.ToArray(),
                IntegralParameterLineDefinition = integralParameterDefinition.ToArray(),
                AverageParameterArchiveLineDefinition = averageParameterArchiveDefinition.ToArray(),
            };

            deviceDefinition = deviceDefinition with { CRC = CRCHelper.GetCRC(deviceDefinition.GetBytes()) };

            cancellationToken.ThrowIfCancellationRequested();

            var existingDeviceDefinition = await deviceRepository.GetDefinitionByDeviceId(device.Id);
            if (existingDeviceDefinition == default)
            {
                await deviceRepository.CreateDefinition(deviceDefinition);

                var parameterDefinitionBytes = bytes.AsSpan(deviceDefinition.ParameterDefinitionStart, DeviceConstants.MAX_PARAMETER_COUNT * DeviceConstants.PARAMETER_SIZE);
                var parameters = dataParser.ParseParameterDefinition(parameterDefinitionBytes, device.Id);
                await parameterRepository.Create(parameters);

                deviceStore.Update(device);
            }
            else
            {
                if (existingDeviceDefinition.CRC != deviceDefinition.CRC)
                {
                    throw new Exception("Device definitions have changed.");
                }
                deviceDefinition = existingDeviceDefinition;
            }

            await dataRepository.InitDataTables(device.Id);

            cancellationToken.ThrowIfCancellationRequested();

            _currentParameters = (await parameterRepository.GetCurrentParametersByDeviceId(device.Id, true)).ToFrozenDictionary(x => x.Number);
            _integralParameters = (await parameterRepository.GetIntegralParametersByDeviceId(device.Id, true)).ToFrozenDictionary(x => x.Number);

            ReadData(bytes, deviceDefinition);

            await ReadHourArchive(bytes, deviceDefinition);

            RaiseConnectionStateChanged(ConnectionState.Disconnected);
        }
        catch (Exception ex)
        {
            RaiseConnectionError(ex);
        }
    }

    private void ReadData(ReadOnlyMemory<byte> bytes, DeviceDefinition deviceDefinition)
    {
        IReadOnlyDictionary<byte, ParameterValue> ReadParameterValues(ushort lineStart, ushort lineSize, byte[] parameterDefinition, ParameterDictionary parameters)
        {
            var parameterLine = bytes.Slice(lineStart, lineSize * 2);
            var current = 0;
            Dictionary<byte, ParameterValue> result = [];
            ushort error = 0;
            foreach (var parameterByte in parameterDefinition)
            {
                if ((parameterByte & 0x80) == 0)
                {
                    var parameter = parameters[parameterByte];
                    var parameterBytes = parameterLine.Slice(current, parameter.Type.GetSize()).Span;
                    var value = dataParser.ParseBytesToString(parameterBytes, parameter);
                    if (parameter.Type == ParameterType.Error)
                    {
                        error = dataParser.ParseUInt16(parameterBytes);
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

        try
        {
            var currentParameterValues = ReadParameterValues(
                deviceDefinition.CurrentParameterLineStart, deviceDefinition.CurrentParameterLineNumber, deviceDefinition.CurrentParameterLineDefinition, _currentParameters);
            var integralParameterValues = ReadParameterValues(
                deviceDefinition.IntegralParameterLineStart, deviceDefinition.IntegralParameterLineLength, deviceDefinition.IntegralParameterLineDefinition, _integralParameters);

            RaiseConnectionDataChanged(currentParameterValues, integralParameterValues);
        }
        catch (Exception ex)
        {
            RaiseConnectionError(ex);
        }
    }

    private async Task ReadHourArchive(ReadOnlyMemory<byte> bytes, DeviceDefinition deviceDefinition)
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
            var readTimeBytes = bytes.Slice(DeviceConstants.CURRENT_DATETIME_START, DeviceConstants.CURRENT_DATETIME_SIZE_REGISTERS * 2);
            var readTime = dataParser.ParseDateTime(readTimeBytes.Span);

            var lineCount = deviceDefinition.AveragePerHourBlockLineCount;
            if (deviceDefinition.LastArchiveRead is not null)
            {
                lineCount = (ushort)int.Clamp((int)(readTime - deviceDefinition.LastArchiveRead.Value).TotalHours, 0, deviceDefinition.AveragePerHourBlockLineCount);
            }

            if (lineCount == 0)
            {
                await CompleteRead(0);
                return;
            }

            var archiveBytes = bytes.Slice(deviceDefinition.AveragePerHourBlockStart, (ushort)(lineCount * deviceDefinition.AverageParameterArchiveLineLength * 2));

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

            var emptyBlock = Enumerable.Repeat<byte>(0, deviceDefinition.AverageParameterArchiveLineLength).ToArray();
            var dateHours = readTime.Date.AddHours(readTime.Hour);
            var actualLineCount = 0;
            for (var i = 0; i < lineCount; ++i)
            {
                var date = dateHours.AddHours(-i);
                var index = lineCount - actualLineCount - 1;
                (data[0] as DataCollection<DateTime>).Values[index] = date;

                var blockBytes = archiveBytes.Slice(i * deviceDefinition.AverageParameterArchiveLineLength, deviceDefinition.AverageParameterArchiveLineLength);
                if (blockBytes.Span.SequenceEqual(emptyBlock))
                {
                    continue;
                }

                var current = 0;
                foreach (var parameterByte in deviceDefinition.AverageParameterArchiveLineDefinition)
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
            await deviceRepository.UpdateDefinitionLastArchiveRead(device.Id, readTime);

            await CompleteRead(actualLineCount);
            RaiseConnectionStateChanged(ConnectionState.Disconnected);
        }
        catch (Exception)
        {
            RaiseConnectionArchiveReadingStateChanged(ArchiveReadingState.None, 0);
            RaiseConnectionArchiveReadingProgressChangedArgs(0, 100);
            throw;
        }
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
