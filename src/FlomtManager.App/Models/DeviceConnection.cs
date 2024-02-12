using DynamicData;
using FlomtManager.Core.Attributes;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;
using FlomtManager.Modbus;
using System.Collections.Frozen;
using System.Reflection;
using System.Timers;
using Timer = System.Timers.Timer;

namespace FlomtManager.App.Models
{
    public class DeviceConnection
    {
        private readonly IDeviceDefinitionRepository _deviceDefinitionRepository;
        private readonly IModbusService _modbusService;

        public int DeviceId { get; set; }
        public IModbusProtocol? ModbusProtocol { get; set; }

        public event EventHandler<DeviceConnectionDataEventArgs>? OnConnectionData;
        public event EventHandler<DeviceConnectionErrorEventArgs>? OnConnectionError;
        public event EventHandler<DeviceConnectionStateEventArgs>? OnConnectionState;

        // parameter type, size in bytes
        private IReadOnlyDictionary<ParameterType, byte> _parameterTypeSizes;

        private ushort _currentParameterLineSize;
        // parameter number, size, comma multiplier
        private IReadOnlyCollection<(byte number, ParameterType type, float comma)> _currentParameterLineDefinition;

        private ushort _integralParameterLineSize;
        // parameter number, size, comma multiplier
        private IReadOnlyCollection<(byte number, ParameterType type, float comma)> _integralParameterLineDefinition;

        private Timer? _dataReadTimer;

        public DeviceConnection(IDeviceDefinitionRepository deviceDefinitionRepository, IModbusService modbusService)
        {
            _deviceDefinitionRepository = deviceDefinitionRepository;
            _modbusService = modbusService;

            var parameterTypeType = typeof(ParameterType);
            _parameterTypeSizes = Enum.GetValues<ParameterType>().ToFrozenDictionary(
                x => x,
                x => parameterTypeType.GetMember(x.ToString()).First().GetCustomAttribute<SizeAttribute>()?.Size ?? throw new Exception("Wrong parameter size."));
        }

        private async void _dataReadTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (ModbusProtocol == null)
            {
                throw new Exception($"{nameof(ModbusProtocol)} is null.");
            }

            await ModbusProtocol.OpenAsync(CancellationToken.None);

            await ModbusProtocol.CloseAsync(CancellationToken.None);
        }

        public bool TryStart(TimeSpan dataReadInterval)
        {
            if (ModbusProtocol == null)
            {
                return false;
            }

            var deviceDefinition = _deviceDefinitionRepository.GetAllQueryable(x => x.DeviceId == DeviceId).FirstOrDefault();
            if (deviceDefinition == null)
            {
                return false;
            }

            // create current parameter line data
            _currentParameterLineDefinition = ParseParameterLineDefinition(deviceDefinition.CurrentParameterLineDefinition);
            _currentParameterLineSize = (ushort)_currentParameterLineDefinition.Sum(x => _parameterTypeSizes[x.type]);

            // create integral parameter line data
            _integralParameterLineDefinition = ParseParameterLineDefinition(deviceDefinition.IntegralParameterLineDefinition);
            _integralParameterLineSize = (ushort)_integralParameterLineDefinition.Sum(x => _parameterTypeSizes[x.type]);
            
            _dataReadTimer = new Timer(dataReadInterval);
            _dataReadTimer.Elapsed += _dataReadTimer_Elapsed;
            _dataReadTimer.Start();
            return true;
        }

        public bool TryStop()
        {
            if (_dataReadTimer == null)
            {
                return false;
            }

            _dataReadTimer.Stop();
            return true;
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
    }
}
