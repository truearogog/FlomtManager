using FlomtManager.App.Models;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;
using FlomtManager.Modbus;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.Concurrent;

namespace FlomtManager.App.Stores
{
    public class DeviceConnectionStore(DeviceStore deviceStore, IDeviceRepository deviceRepository, IModbusService modbusService, 
        IDeviceDefinitionRepository deviceDefinitionRepository, IParameterRepository parameterRepository)
    {
        private readonly DeviceStore _deviceStore = deviceStore;
        private readonly IDeviceRepository _deviceRepository = deviceRepository;
        private readonly IModbusService _modbusService = modbusService;
        private readonly IDeviceDefinitionRepository _deviceDefinitionRepository = deviceDefinitionRepository;
        private readonly IParameterRepository _parameterRepository = parameterRepository;

        // devices currently trying to connect
        private ConcurrentDictionary<int, byte> _connectingDevices = [];
        // connected devices: device id, connection
        private ConcurrentDictionary<int, DeviceConnection> _deviceConnections = [];

        public event EventHandler<DeviceConnectionStateEventArgs>? OnDeviceConnectionState;
        public event EventHandler<DeviceConnectionErrorEventArgs>? OnDeviceConnectionError;

        public ConnectionState GetConnectionState(int deviceId)
        {
            if (_connectingDevices.ContainsKey(deviceId))
            {
                return ConnectionState.Connecting;
            }
            if (_deviceConnections.ContainsKey(deviceId))
            {
                return ConnectionState.Connected;
            }
            return ConnectionState.Disconnected;
        }

        public async Task TryConnect(Device device, EventHandler<DeviceConnectionDataEventArgs> dataEventHandler, CancellationToken cancellationToken = default)
        {
            IModbusProtocol? modbusProtocol = null;
            try
            {
                if (!_connectingDevices.TryAdd(device.Id, default))
                {
                    throw new InvalidOperationException("Device already is connecting.");
                }

                OnDeviceConnectionState?.Invoke(this, new DeviceConnectionStateEventArgs
                {
                    DeviceId = device.Id,
                    ConnectionState = ConnectionState.Connecting,
                });

                // create modbus protocol
                modbusProtocol = device.ConnectionType switch
                {
                    ConnectionType.Serial => new ModbusProtocolSerial(
                        device.PortName ?? throw new ModbusException("Device should have Port Name."), device.BaudRate, device.Parity, device.DataBits, device.StopBits),
                    ConnectionType.Network => new ModbusProtocolTcp(
                        device.IpAddress ?? throw new ModbusException("Device should have IP Address."), device.Port),
                    _ => throw new NotSupportedException("Not supported device connection type.")
                };

                await modbusProtocol.OpenAsync(cancellationToken);
                var deviceDefinition = await _modbusService.ReadDeviceDefinition(modbusProtocol, device.SlaveId, cancellationToken);
                deviceDefinition.DeviceId = device.Id;

                // if first time - read device definition and parameter definitions
                if (device.DeviceDefinitionId == 0)
                {
                    var deviceDefinitionId = await _deviceDefinitionRepository.Create(deviceDefinition);
                    device.DeviceDefinitionId = deviceDefinitionId;
                    await _deviceStore.UpdateDevice(_deviceRepository, device);

                    var parameters = await _modbusService.ReadParameterDefinitions(modbusProtocol, device.SlaveId, deviceDefinition, cancellationToken);
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
                        throw new ModbusException("Device definitions have been changed.");
                    }
                }
                await modbusProtocol.CloseAsync(cancellationToken);

                // start device connection
                var deviceConnection = App.Host.Services.GetRequiredService<DeviceConnection>();
                deviceConnection.SlaveId = device.SlaveId;
                deviceConnection.DeviceDefinition = deviceDefinition;
                deviceConnection.ModbusProtocol = modbusProtocol;
                deviceConnection.OnConnectionError += _OnConnectionError;
                deviceConnection.OnConnectionData += dataEventHandler;

                deviceConnection.TryStart(TimeSpan.FromSeconds(5));
                _deviceConnections.TryAdd(device.Id, deviceConnection);

                OnDeviceConnectionState?.Invoke(this, new DeviceConnectionStateEventArgs
                {
                    DeviceId = device.Id,
                    ConnectionState = ConnectionState.Connected,
                });
            }
            // cleanup
            catch (Exception ex)
            {
                if (modbusProtocol?.IsOpen == true)
                {
                    await modbusProtocol.CloseAsync(CancellationToken.None);
                }

                OnDeviceConnectionState?.Invoke(this, new DeviceConnectionStateEventArgs
                {
                    DeviceId = device.Id,
                    ConnectionState = ConnectionState.Disconnected,
                });
                Log.Error(string.Empty, ex);
                _connectingDevices.TryRemove(device.Id, out _);
                throw;
            }
            // inner finally is not executed after rethrow if not caught
            finally
            {
                _connectingDevices.TryRemove(device.Id, out _);
            }
        }

        private void _OnConnectionError(object? sender, DeviceConnectionErrorEventArgs e)
        {
            _deviceConnections.Remove(e.DeviceId, out var connection);
            if (connection != null)
            {
                connection.TryStop();
                connection.OnConnectionError -= _OnConnectionError;
            }
            OnDeviceConnectionState?.Invoke(this, new DeviceConnectionStateEventArgs
            {
                DeviceId = e.DeviceId,
                ConnectionState = ConnectionState.Disconnected,
            });
            OnDeviceConnectionError?.Invoke(this, new DeviceConnectionErrorEventArgs
            {
                DeviceId = e.DeviceId,
                Exception = e.Exception,
            });
        }

        private void _OnConnectionData(object? sender, DeviceConnectionDataEventArgs e)
        {

        }
    }
}
