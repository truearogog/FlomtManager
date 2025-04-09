using System.Collections.Frozen;
using System.Collections.ObjectModel;
using FlomtManager.Domain.Abstractions.Parsers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;
using FlomtManager.Domain.Models.Collections;
using ReactiveUI;

namespace FlomtManager.Application.ViewModels;

internal sealed class DataTableViewModel(
    IDataFormatter dataFormatter,
    IDataRepository dataRepository,
    IParameterRepository parameterRepository,
    IParameterViewModelFactory parameterViewModelFactory) : ViewModel, IDataTableViewModel
{
    public event EventHandler OnDataUpdated;

    private ObservableCollection<StringValueCollection> _data;
    public ObservableCollection<StringValueCollection> Data
    {
        get => _data;
        private set => this.RaiseAndSetIfChanged(ref _data, value);
    }

    public IReadOnlyDictionary<byte, byte> ParameterPositions { get; private set; }

    private Device _device;
    public Device Device
    {
        get => _device;
        private set => this.RaiseAndSetIfChanged(ref _device, value);
    }

    private IParameterViewModel _dateTimeParameter;
    public IParameterViewModel DateTimeParameter
    {
        get => _dateTimeParameter;
        private set => this.RaiseAndSetIfChanged(ref _dateTimeParameter, value);
    }

    private ObservableCollection<IParameterViewModel> _parameters = [];
    public ObservableCollection<IParameterViewModel> Parameters
    {
        get => _parameters;
        private set => this.RaiseAndSetIfChanged(ref _parameters, value);
    }

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

        DateTimeParameter = parameterViewModelFactory.Create(new()
        {
            Number = 0,
            Comma = default,
            ErrorMask = default,
            IntegrationNumber = default,
            Name = "Time",
            Unit = string.Empty,
            Color = "#FF6B7075",
            Type = ParameterType.Time,
            IsAxisVisibleOnChart = false,
            IsAutoScaledOnChart = true,
            ZoomLevelOnChart = 0,
        }, false);

        Parameters.Clear();
        var parameters = await parameterRepository.GetHourArchiveParametersByDeviceId(Device.Id);
        foreach (var parameter in parameters)
        {
            var viewModel = parameterViewModelFactory.Create(parameter, false);
            Parameters.Add(viewModel);
        }
    }

    public async Task UpdateData()
    {
        if (!await dataRepository.HasHourData(Device.Id))
        {
            return;
        }

        var parameters = (await parameterRepository.GetHourArchiveParametersByDeviceId(Device.Id)).ToFrozenDictionary(x => x.Number);
        var dataCollections = (await dataRepository.GetHourData(Device.Id))
            .Where(x => x.Key == 0 || parameters.ContainsKey(x.Key)).ToFrozenDictionary();
        var size = dataCollections.First().Value.Count;

        var parameterPositions = new Dictionary<byte, byte>();
        byte index = 0;
        foreach (var (number, parameter) in parameters.Where(x => x.Key != 0))
        {
            parameterPositions.Add(parameter.Number, index);
            index++;
        }

        var data = new StringValueCollection[size];

        for (var i = 0; i < size; ++i)
        {
            index = 0;
            var collection = new StringValueCollection
            {
                Values = new string[dataCollections.Count - 1]
            };
            foreach (var (parameterNumber, dataCollection) in dataCollections)
            {
                if (parameterNumber == 0)
                {
                    if (dataCollection is DataCollection<DateTime> dateTimeDataCollection)
                    {
                        collection.DateTime = dateTimeDataCollection.Values[i];
                        collection.DateTimeString = dataFormatter.FormatDateTime(dateTimeDataCollection.Values[i]);
                    }
                }
                else
                {
                    if (dataCollection is DataCollection<float> floatDataCollection)
                    {
                        var parameter = parameters[parameterNumber];
                        collection.Values[index] = dataFormatter.FormatFloat(floatDataCollection.Values[i], parameter);
                    }
                    if (dataCollection is DataCollection<uint> uintDataCollection)
                    {
                        collection.Values[index] = dataFormatter.FormatUInt32(uintDataCollection.Values[i]);
                    }
                    if (dataCollection is DataCollection<ushort> ushortDataCollection)
                    {
                        collection.Values[index] = dataFormatter.FormatUInt16(ushortDataCollection.Values[i]);
                    }
                    if (dataCollection is DataCollection<TimeSpan> timeSpanDataCollection)
                    {
                        collection.Values[index] = dataFormatter.FormatTimeSpan(timeSpanDataCollection.Values[i]);
                    }
                    if (dataCollection is DataCollection<DateTime> dateTimeDataCollection)
                    {
                        collection.Values[index] = dataFormatter.FormatDateTime(dateTimeDataCollection.Values[i]);
                    }
                    ++index;
                }
            }
            data[i] = collection;
        }

        Data = new(data.OrderByDescending(x => x.DateTime));
        ParameterPositions = parameterPositions;
        OnDataUpdated?.Invoke(this, EventArgs.Empty);
    }
}
