using System.Collections.Frozen;
using System.Collections.ObjectModel;
using FlomtManager.Domain.Abstractions.Parsers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;
using FlomtManager.Domain.Models.Collections;
using ReactiveUI;
using static FlomtManager.Domain.Abstractions.ViewModels.IDataTableViewModel;

namespace FlomtManager.Application.ViewModels;

internal sealed class DataTableViewModel(
    IDataFormatter dataFormatter,
    IDataRepository dataRepository,
    IParameterRepository parameterRepository) : ViewModel, IDataTableViewModel
{
    public event EventHandler OnDataUpdated;

    public ObservableCollection<ValueCollection> Data { get; private set; }
    public IReadOnlyDictionary<byte, byte> ParameterPositions { get; private set; }

    private Device _device;
    public Device Device
    {
        get => _device;
        set => this.RaiseAndSetIfChanged(ref _device, value);
    }

    public List<Parameter> Parameters { get; } = [];

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
            Color = "#FF6B7075",
            Type = ParameterType.Time,
            YAxisIsVisible = false,
            YAxisScalingType = ChartScalingType.Auto,
            YAxisZoom = 1
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
