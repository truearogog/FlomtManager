using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Skia;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Models.Collections;
using FlomtManager.Core.Parsers;
using FlomtManager.Core.Repositories;
using ReactiveUI;

namespace FlomtManager.App.ViewModels
{
    public class DataGroupTableViewModel(
        IDataRepository dataRepository,
        IParameterRepository parameterRepository,
        IDataFormatter dataFormatter) : ViewModel
    {
        public struct ValueCollection(int size)
        {
            public object[] Values { get; } = new object[size];
        }

        public ObservableCollection<ValueCollection> Data { get; private set; }
        public IReadOnlyDictionary<byte, byte> ParameterPositions { get; private set; }

        private Device _device;
        public Device Device
        {
            get => _device;
            set => this.RaiseAndSetIfChanged(ref _device, value);
        }

        public event EventHandler OnDataUpdated;

        public List<Parameter> Parameters { get; private set; } = [];


        public async Task SetDevice(Device device)
        {
            Device = device;
            await UpdateParameters();
        }

        private async Task UpdateParameters()
        {
            if (Device == default)
            {
                return;
            }

            Parameters.Clear();
            Parameters.Add(new()
            {
                Number = 0,
                Comma = default,
                ErrorMask = default,
                IntegrationNumber = default,
                Name = "Time",
                Unit = string.Empty,
                Color = Avalonia.Media.Colors.DarkGray.ToSKColor().ToString(),
                Type = ParameterType.Time,
            });

            var parameters = await parameterRepository.GetHourArchiveParametersByDeviceId(Device.Id);
            foreach (var parameter in parameters)
            {
                Parameters.Add(parameter);
            }
        }

        public async Task UpdateData()
        {
            if (!await dataRepository.HasHourData(Device.Id))
            {
                return;
            }

            var dataCollections = (await dataRepository.GetHourData(Device.Id)).Where(x => Parameters.Any(p => p.Number == x.Key)).ToFrozenDictionary();
            var size = dataCollections.First().Value.Count;

            var parameterPositions = new Dictionary<byte, byte>();
            var parameterPositionsInitialized = false;

            var data = new ValueCollection[size];

            for (var i = 0; i < size; ++i)
            {
                var valueCollection = new ValueCollection(dataCollections.Count);
                byte index = 0;
                foreach (var (parameterNumber, dataCollection) in dataCollections)
                {
                    if (dataCollection is DataCollection<float> floatDataCollection)
                    {
                        valueCollection.Values[index] = floatDataCollection.Values[i];
                    }
                    if (dataCollection is DataCollection<uint> uintDataCollection)
                    {
                        valueCollection.Values[index] = uintDataCollection.Values[i];
                    }
                    if (dataCollection is DataCollection<ushort> ushortDataCollection)
                    {
                        valueCollection.Values[index] = ushortDataCollection.Values[i];
                    }
                    if (dataCollection is DataCollection<TimeSpan> timeSpanDataCollection)
                    {
                        valueCollection.Values[index] = dataFormatter.FormatTimeSpan(timeSpanDataCollection.Values[i]);
                    }
                    if (dataCollection is DataCollection<DateTime> dateTimeDataCollection)
                    {
                        valueCollection.Values[index] = dateTimeDataCollection.Values[i];
                    }

                    if (!parameterPositionsInitialized)
                    {
                        parameterPositions.Add(parameterNumber, index);
                    }

                    ++index;
                }

                parameterPositionsInitialized = true;
                data[i] = valueCollection;
            }

            Data = new(data);
            ParameterPositions = parameterPositions;
            OnDataUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
