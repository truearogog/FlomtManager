﻿using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using FlomtManager.App.Models;
using FlomtManager.App.Stores;
using FlomtManager.Core.Attributes;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;
using FlomtManager.Framework.Extensions;
using FlomtManager.Modbus;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace FlomtManager.App.ViewModels
{
    public class DeviceViewModel : ViewModelBase
    {
        private readonly IModbusService _modbusService;
        private readonly DeviceStore _deviceStore;
        private readonly IDeviceDefinitionRepository _deviceDefinitionRepository;
        private readonly IParameterRepository _parameterRepository;

        public event EventHandler? CloseRequested;
        public event EventHandler<Device>? DeviceUpdateRequested;
        public event EventHandler<(NotificationType, string)>? NotificationRequested;

        private Device? _device;
        public Device? Device
        {
            get => _device;
            set 
            {
                this.RaiseAndSetIfChanged(ref _device, value);
            }
        }

        public ObservableCollection<ParameterViewModel> CurrentParameters { get; set; } = [];
        public ObservableCollection<ParameterViewModel> IntegralParameters { get; set; } = [];

        public DeviceConnectionViewModel? DeviceConnection { get; set; }

        public DeviceViewModel(IModbusService modbusService, DeviceStore deviceStore, IDeviceDefinitionRepository deviceDefinitionRepository, IParameterRepository parameterRepository)
        {
            _modbusService = modbusService;
            _deviceStore = deviceStore;
            _deviceDefinitionRepository = deviceDefinitionRepository;
            _parameterRepository = parameterRepository;

            _deviceStore.DeviceUpdated += _DeviceUpdated;
            _deviceStore.DeviceDeleted += _DeviceDeleted;

            DeviceConnection = App.Host.Services.GetRequiredService<DeviceConnectionViewModel>();
            DeviceConnection.OnConnectionData += _OnConnectionData;
            DeviceConnection.OnConnectionError += _OnConnectionError;
        }

        private async void AddParameters()
        {
            if (Device == null)
            {
                return;
            }

            var parameters = await _parameterRepository.GetAll(x => x.DeviceId == Device.Id);

            var deviceDefinition = Device.DeviceDefinition ?? _deviceDefinitionRepository.GetAllQueryable(x => x.DeviceId == Device.Id).FirstOrDefault();
            if (deviceDefinition == null)
            {
                return;
            }

            AddParameters(deviceDefinition.CurrentParameterLineDefinition!, CurrentParameters, parameters);            
            AddParameters(deviceDefinition.IntegralParameterLineDefinition!, IntegralParameters, parameters);            
        }

        private void AddParameters(byte[] parameterLineDefinition, ObservableCollection<ParameterViewModel> parameterCollection, IEnumerable<Parameter> parameters)
        {
            parameterCollection.Clear();
            foreach (var parameterByte in parameterLineDefinition)
            {
                if ((parameterByte & 0x80) == 0)
                {
                    var parameter = parameters.First(x => x.Number == parameterByte);
                    if (parameter.ParameterType.GetAttribute<HideAttribute>() == null)
                    {
                        parameterCollection.Add(new ParameterViewModel
                        {
                            Number = parameter.Number,
                            Name = parameter.Name,
                            Unit = parameter.Unit,
                        });
                    }
                }
            }
        }

        public void UpdateDevice(Device device)
        {
            DeviceUpdateRequested?.Invoke(this, device);
        }

        private void _DeviceUpdated(Device device)
        {
            if (Device?.Id == device.Id)
            {
                Device = device;
            }
        }

        private void _DeviceDeleted(int id)
        {
            if (Device?.Id == id)
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
                TryDisconnect();
            }
        }

        private CancellationTokenSource? _connectionCancellationTokenSource;
        public async void TryConnect()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(Device);
                ArgumentNullException.ThrowIfNull(DeviceConnection);
                _connectionCancellationTokenSource = new CancellationTokenSource();
                await DeviceConnection.Connect(Device, _connectionCancellationTokenSource.Token);
                AddParameters();
            }
            catch (ModbusException ex)
            {
                RequestNotification(NotificationType.Error, ex.Message);
            }
            catch (OperationCanceledException)
            {
                RequestNotification(NotificationType.Warning, "Connection cancelled.");
            }
            catch
            {
                RequestNotification(NotificationType.Error, "Connection error.");
            }
        }

        public void TryCancelConnect()
        {
            ArgumentNullException.ThrowIfNull(_connectionCancellationTokenSource);
            _connectionCancellationTokenSource.Cancel();
        }

        public async void TryDisconnect()
        {
            _connectionCancellationTokenSource?.Cancel();
            ArgumentNullException.ThrowIfNull(DeviceConnection);
            await DeviceConnection.Disconnect();
        }

        private void _OnConnectionData(object? sender, DeviceConnectionDataEventArgs e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                foreach (var currentParameter in CurrentParameters)
                {
                    ParameterValue? value = e.CurrentParameters.TryGetValue(currentParameter.Number, out var _value) ? _value : null;
                    if (value != null)
                    {
                        currentParameter.Value = value.Value;
                        currentParameter.Error = value.Error;
                    }
                }
                foreach (var integralParameter in IntegralParameters)
                {
                    ParameterValue? value = e.IntegralParameters.TryGetValue(integralParameter.Number, out var _value) ? _value : null;
                    if (value != null)
                    {
                        integralParameter.Value = value.Value;
                        integralParameter.Error = value.Error;
                    }
                }
            });
        }

        private void _OnConnectionError(object? sender, DeviceConnectionErrorEventArgs e)
        {
            if (e.Exception is ModbusException ex)
            {
                RequestNotification(NotificationType.Error, ex.Message);
            }
            else if (e.Exception is OperationCanceledException)
            {
                RequestNotification(NotificationType.Error, "Connection cancelled.");
            }
            else
            {
                RequestNotification(NotificationType.Error, "Connection error.");
            }
        }

        private void RequestNotification(NotificationType type, string message)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                NotificationRequested?.Invoke(this, (type, message));
            });
        }
    }
}
