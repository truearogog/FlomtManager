using FlomtManager.Core.Attributes;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Services;
using FlomtManager.Framework.Extensions;
using FlomtManager.Modbus;
using Serilog;
using System.Collections.Frozen;
using System.Reflection;
using System.Timers;
using Timer = System.Timers.Timer;

namespace FlomtManager.App.Models
{
    public class DeviceConnection
    {
        private readonly IModbusService _modbusService;

        public byte SlaveId { get; set; }
        public DeviceDefinition? DeviceDefinition { get; set; }
        public IModbusProtocol? ModbusProtocol { get; set; }

        public event EventHandler<DeviceConnectionDataEventArgs>? OnConnectionData;
        public event EventHandler<DeviceConnectionErrorEventArgs>? OnConnectionError;
        public event EventHandler<DeviceConnectionStateEventArgs>? OnConnectionState;

        // parameter type, size in bytes
        private IReadOnlyDictionary<ParameterType, byte> _parameterTypeSizes;

        private ushort _currentParameterLineSize;
        // parameter number, size, comma multiplier
        private IReadOnlyCollection<(byte number, ParameterType type, float comma)>? _currentParameterLineDefinition;

        private ushort _integralParameterLineSize;
        // parameter number, size, comma multiplier
        private IReadOnlyCollection<(byte number, ParameterType type, float comma)>? _integralParameterLineDefinition;

        private Timer? _dataReadTimer;
        private CancellationTokenSource _cancellationTokenSource;

        public DeviceConnection(IModbusService modbusService)
        {
            _modbusService = modbusService;

            var parameterTypeType = typeof(ParameterType);
            _parameterTypeSizes = Enum.GetValues<ParameterType>().ToFrozenDictionary(
                x => x,
                x => parameterTypeType.GetMember(x.ToString()).First().GetCustomAttribute<SizeAttribute>()?.Size ?? throw new Exception("Wrong parameter size."));

            _cancellationTokenSource = new CancellationTokenSource();
        }

        private int _lockFlag = 0; // 0 - free
        private async void _dataReadTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (Interlocked.CompareExchange(ref _lockFlag, 1, 0) == 0)
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(ModbusProtocol);
                    ArgumentNullException.ThrowIfNull(DeviceDefinition);

                    var currentParameters = await ReadParameterLine(_currentParameterLineDefinition!, DeviceDefinition.CurrentParameterLineStart, 
                        _currentParameterLineSize, _cancellationTokenSource.Token);
                    var integralParameters = await ReadParameterLine(_integralParameterLineDefinition!, DeviceDefinition.IntegralParameterLineStart, 
                        _integralParameterLineSize, _cancellationTokenSource.Token);

                    OnConnectionData?.Invoke(this, new DeviceConnectionDataEventArgs
                    {
                        DeviceId = DeviceDefinition.DeviceId,
                        CurrentParameters = currentParameters,
                        IntegralParameters = integralParameters
                    });

                }
                catch (OperationCanceledException)
                {
                    await TryStop();
                }
                catch (Exception ex)
                {
                    await TryStop();
                    OnConnectionError?.Invoke(this, new DeviceConnectionErrorEventArgs
                    {
                        DeviceId = DeviceDefinition!.DeviceId,
                        Exception = ex
                    });
                    Log.Error(string.Empty, ex);
                }
                finally
                {
                    Interlocked.Decrement(ref _lockFlag);
                }
            }
        }

        public async Task<bool> TryStart(TimeSpan dataReadInterval)
        {
            if (ModbusProtocol == null || DeviceDefinition == null)
            {
                return false;
            }
            await ModbusProtocol.OpenAsync(CancellationToken.None);

            // create current parameter line data
            _currentParameterLineDefinition = ParseParameterLineDefinition(DeviceDefinition.CurrentParameterLineDefinition);
            _currentParameterLineSize = (ushort)_currentParameterLineDefinition.Sum(x => _parameterTypeSizes[x.type]);

            // create integral parameter line data
            _integralParameterLineDefinition = ParseParameterLineDefinition(DeviceDefinition.IntegralParameterLineDefinition);
            _integralParameterLineSize = (ushort)_integralParameterLineDefinition.Sum(x => _parameterTypeSizes[x.type]);

            _dataReadTimer = new Timer(dataReadInterval);
            _dataReadTimer.Elapsed += _dataReadTimer_Elapsed;
            _dataReadTimer.Start();
            return true;
        }

        public async Task<bool> TryStop()
        {
            if (_dataReadTimer == null || ModbusProtocol == null)
            {
                return false;
            }
            _dataReadTimer.Stop();
            await ModbusProtocol.CloseAsync(CancellationToken.None);
            return true;
        }

        public async Task<bool> TryStopIfNoListeners()
        {
            if (OnConnectionData?.GetInvocationList().Length == 0)
            {
                return await TryStop();
            }
            return false;
        }

        private IReadOnlyCollection<(byte, ParameterType, float)> ParseParameterLineDefinition(byte[] bytes)
        {
            List<(byte number, ParameterType type, float comma)> parameterTypes = [];
            for (var i = 0; i < bytes.Length; i += 2)
            {
                var (type, comma) = _modbusService.ParseParameterTypeByte(bytes[i + 1]);
                parameterTypes.Add((bytes[i], type, comma));
            }
            return parameterTypes.AsReadOnly();
        }

        private async Task<IReadOnlyDictionary<byte, string>> ReadParameterLine(
            IReadOnlyCollection<(byte, ParameterType, float)> parameterLineDefinition, ushort lineStart, ushort lineSize, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ModbusProtocol);
            ArgumentNullException.ThrowIfNull(DeviceDefinition);

            var parameterLine = await ModbusProtocol.ReadRegistersBytesAsync(SlaveId, lineStart, (ushort)(lineSize / 2), cancellationToken: cancellationToken);
            var current = 0;
            Dictionary<byte, string> result = [];
            foreach (var (number, type, comma) in parameterLineDefinition)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (number != 0)
                {
                    var value = ParseBytesToValue(parameterLine.AsSpan(current, _parameterTypeSizes[type]), type, comma);
                    result.Add(number, value);
                }
                current += _parameterTypeSizes[type];
            }
            return result.AsReadOnly();
        }

        private static string ParseBytesToValue(ReadOnlySpan<byte> bytes, ParameterType type, float comma)
        {
            return type switch
            {
                ParameterType.S16C => BitConverter.ToInt16(bytes).ToString(),
                ParameterType.U16C => BitConverter.ToUInt16(bytes).ToString(),
                ParameterType.FS16C => ((BitConverter.ToUInt16(bytes) & 0x3FFF) * (((bytes[0] >> 6) & 1) == 0 ? 1 : -1) * MathF.Pow(10, -(bytes[1] >> 7))).ToString(),
                ParameterType.FU16C => ((BitConverter.ToUInt16(bytes) & 0x3FFF) * MathF.Pow(10, -(bytes[1] >> 6))).ToString(),
                ParameterType.S32C => BitConverter.ToSingle(bytes).ToString(),
                ParameterType.S32CD1 => BitConverter.ToSingle(bytes).TrimDecimalPlaces(1).ToString(),
                ParameterType.S32CD2 => BitConverter.ToSingle(bytes).TrimDecimalPlaces(2).ToString(),
                ParameterType.S32CD3 => BitConverter.ToSingle(bytes).TrimDecimalPlaces(1).ToString(),
                ParameterType.Error => BitConverter.ToUInt16(bytes).ToString(),
                _ => string.Empty
            };
        }
    }
}
