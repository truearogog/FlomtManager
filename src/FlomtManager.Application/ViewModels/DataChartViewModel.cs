﻿using System.Collections.ObjectModel;
using System.Reactive.Linq;
using FlomtManager.Domain.Abstractions.Parsers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Abstractions.ViewModels.Events;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;
using FlomtManager.Domain.Models.Collections;
using FlomtManager.Framework.Extensions;
using FlomtManager.Framework.Helpers;
using ReactiveUI;

namespace FlomtManager.Application.ViewModels;

internal sealed class DataChartViewModel : ViewModel, IDataChartViewModel
{
    private readonly IDataFormatter _dataFormatter;
    private readonly IDataRepository _dataRepository;
    private readonly IParameterViewModelFactory _parameterViewModelFactory;
    private readonly IParameterRepository _parameterRepository;
    private readonly IParameterStore _parameterStore;

    private IReadOnlyDictionary<Parameter, Parameter> _integralParameters;
    private float[] _workingTimeCollectionFloat;
    private uint[] _workingTimeCollectionUint;
    private ushort[] _workingTimeCollectionUshort;

    public event EventHandler<Parameter> OnParameterUpdated;
    public event EventHandler OnDataUpdated;
    public event EventHandler<IntegrationChangedArgs> OnIntegrationChanged;

    public IEnumerable<Parameter> VisibleParameters { get; private set; }
    public IReadOnlyDictionary<byte, IDataCollection> DataCollections { get; private set; }
    public double[] DateTimes { get; private set; }

    private Device _device;
    public Device Device
    {
        get => _device;
        set => this.RaiseAndSetIfChanged(ref _device, value);
    }

    public ObservableCollection<IParameterViewModel> Parameters { get; set; } = [];

    private bool _integrationSpanActive;
    public bool IntegrationSpanActive
    {
        get => _integrationSpanActive;
        set => this.RaiseAndSetIfChanged(ref _integrationSpanActive, value);
    }

    private double _integrationSpanMinDate;
    public double IntegrationSpanMinDate
    {
        get => _integrationSpanMinDate;
        private set => this.RaiseAndSetIfChanged(ref _integrationSpanMinDate, value);
    }

    private double _integrationSpanMaxDate;
    public double IntegrationSpanMaxDate
    {
        get => _integrationSpanMaxDate;
        private set => this.RaiseAndSetIfChanged(ref _integrationSpanMaxDate, value);
    }

    private int _integrationSpanMinIndex;
    public int IntegrationSpanMinIndex
    {
        get => _integrationSpanMinIndex;
        private set => this.RaiseAndSetIfChanged(ref _integrationSpanMinIndex, value);
    }

    private int _integrationSpanMaxIndex;
    public int IntegrationSpanMaxIndex
    {
        get => _integrationSpanMaxIndex;
        private set => this.RaiseAndSetIfChanged(ref _integrationSpanMaxIndex, value);
    }

    private double _currentDisplayDate;
    public double CurrentDisplayDate
    {
        get => _currentDisplayDate;
        set => this.RaiseAndSetIfChanged(ref _currentDisplayDate, value);
    }

    private double _currentDisplaySpanMinDate;
    public double CurrentDisplaySpanMinDate
    {
        get => _currentDisplaySpanMinDate;
        private set => this.RaiseAndSetIfChanged(ref _currentDisplaySpanMinDate, value);
    }

    private double _currentDisplaySpanMaxDate;
    public double CurrentDisplaySpanMaxDate
    {
        get => _currentDisplaySpanMaxDate;
        private set => this.RaiseAndSetIfChanged(ref _currentDisplaySpanMaxDate, value);
    }

    private int _currentDisplaySpanMinIndex;
    public int CurrentDisplaySpanMinIndex
    {
        get => _currentDisplaySpanMinIndex;
        private set => this.RaiseAndSetIfChanged(ref _currentDisplaySpanMinIndex, value);
    }

    private int _currentDisplaySpanMaxIndex;
    public int CurrentDisplaySpanMaxIndex
    {
        get => _currentDisplaySpanMaxIndex;
        private set => this.RaiseAndSetIfChanged(ref _currentDisplaySpanMaxIndex, value);
    }

    public DataChartViewModel(
        IDataFormatter dataFormatter,
        IDataRepository dataRepository,
        IParameterViewModelFactory parameterViewModelFactory,
        IParameterRepository parameterRepository,
        IParameterStore parameterStore)
    {
        _dataFormatter = dataFormatter;
        _dataRepository = dataRepository;
        _parameterViewModelFactory = parameterViewModelFactory;
        _parameterRepository = parameterRepository;
        _parameterStore = parameterStore;

        _parameterStore.Updated += _parameterStore_Updated;

        this.WhenAnyValue(x => x.CurrentDisplayDate)
            .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => ChangeSelectedDataPoint());
    }

    private void _parameterStore_Updated(object sender, Parameter e)
    {
        if (Device == default || e.DeviceId != Device.Id)
        {
            return;
        }

        OnParameterUpdated?.Invoke(this, e);
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

        Parameters.Clear();

        var dateTimeParameterViewModel = _parameterViewModelFactory.Create(new()
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
        }, false, false);
        Parameters.Add(dateTimeParameterViewModel);

        var parameters = await _parameterRepository.GetHourArchiveParametersByDeviceId(Device.Id);
        foreach (var parameter in parameters)
        {
            var parameterViewModel = _parameterViewModelFactory.Create(parameter, true, true);
            Parameters.Add(parameterViewModel);
        }

        VisibleParameters = parameters;

        _integralParameters = await _parameterRepository.GetHourArchiveIntegralParametersByDeviceId(Device.Id);
    }

    public async Task UpdateData()
    {
        if (!await _dataRepository.HasHourData(Device.Id))
        {
            return;
        }

        var dataCollections = await _dataRepository.GetHourData(Device.Id);
        var parameters = await _parameterRepository.GetHourArchiveParametersByDeviceId(Device.Id, true);

        var workingTimeParameter = parameters.FirstOrDefault(x => x.Type == ParameterType.WorkingTimeInSecondsInArchiveInterval);
        if (workingTimeParameter == default)
        {
            return;
        }

        DateTimes = ((DataCollection<DateTime>)dataCollections[0]).Values.Select(x => x.ToOADate()).ToArray();

        var workingTimeCollection = (DataCollection<ushort>)dataCollections[workingTimeParameter.Number];
        _workingTimeCollectionFloat = workingTimeCollection.Values.Select(x => (float)x).ToArray();
        _workingTimeCollectionUint = workingTimeCollection.Values.Select(x => (uint)x).ToArray();
        _workingTimeCollectionUshort = workingTimeCollection.Values;
        DataCollections = dataCollections;

        OnDataUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateCurrentDisplaySpanDates(double min, double max)
    {
        CurrentDisplaySpanMinDate = min;
        CurrentDisplaySpanMaxDate = max;

        CurrentDisplaySpanMinIndex = DateTimes.BinarySearchClosestValueIndex(min);
        CurrentDisplaySpanMaxIndex = DateTimes.BinarySearchClosestValueIndex(max);
    }

    public void UpdateIntegration(double min, double max)
    {
        if (DateTimes is null || DataCollections is null || min == default || max == default)
        {
            return;
        }

        IntegrationSpanMinDate = min;
        IntegrationSpanMaxDate = max;

        IntegrationSpanMinIndex = DateTimes.BinarySearchClosestValueIndex(min);
        IntegrationSpanMaxIndex = DateTimes.BinarySearchClosestValueIndex(max);

        Dictionary<byte, string> integrationResults = [];

        foreach (var (parameter, integrationParameter) in _integralParameters)
        {
            if (IntegrationSpanMinIndex > IntegrationSpanMaxIndex || IntegrationSpanMaxIndex > DataCollections[parameter.Number].Count - 1)
            {
                integrationResults[integrationParameter.Number] = string.Empty;
                continue;
            }

            if (DataCollections[parameter.Number] is DataCollection<float> floatDataCollection)
            {
                var sum = MathHelper.Integrate(
                    floatDataCollection.Values.AsSpan(IntegrationSpanMinIndex, IntegrationSpanMaxIndex - IntegrationSpanMinIndex), 
                    _workingTimeCollectionFloat.AsSpan(IntegrationSpanMinIndex, IntegrationSpanMaxIndex - IntegrationSpanMinIndex));
                integrationResults[integrationParameter.Number] = _dataFormatter.FormatFloat(sum, integrationParameter);
            }
            else if (DataCollections[parameter.Number] is DataCollection<uint> uintDataCollection)
            {
                var sum = MathHelper.Integrate(
                    uintDataCollection.Values.AsSpan(IntegrationSpanMinIndex, IntegrationSpanMaxIndex - IntegrationSpanMinIndex), 
                    _workingTimeCollectionUint.AsSpan(IntegrationSpanMinIndex, IntegrationSpanMaxIndex - IntegrationSpanMinIndex));
                integrationResults[integrationParameter.Number] = _dataFormatter.FormatUInt32(sum);
            }
            else if (DataCollections[parameter.Number] is DataCollection<ushort> ushortDataCollection)
            {
                var sum = MathHelper.Integrate(
                    ushortDataCollection.Values.AsSpan(IntegrationSpanMinIndex, IntegrationSpanMaxIndex - IntegrationSpanMinIndex), 
                    _workingTimeCollectionUshort.AsSpan(IntegrationSpanMinIndex, IntegrationSpanMaxIndex - IntegrationSpanMinIndex));
                integrationResults[integrationParameter.Number] = _dataFormatter.FormatUInt16(sum);
            }
        }

        OnIntegrationChanged?.Invoke(this, new IntegrationChangedArgs
        {
            IntegrationStart = DateTime.FromOADate(IntegrationSpanMinDate),
            IntegrationEnd = DateTime.FromOADate(IntegrationSpanMaxDate),
            IntegratedValues = integrationResults,
        });
    }

    private void ChangeSelectedDataPoint()
    {
        if (DateTimes is null || DataCollections is null)
        {
            return;
        }

        var index = DateTimes.BinarySearchClosestValueIndex(CurrentDisplayDate);
        if (index != -1)
        {
            foreach (var parameterViewModel in Parameters)
            {
                var parameter = parameterViewModel.Parameter;
                if (DataCollections[parameter.Number] is DataCollection<float> floatDataCollection)
                {
                    parameterViewModel.Value = _dataFormatter.FormatFloat(floatDataCollection.Values[index], parameter);
                }
                else if (DataCollections[parameter.Number] is DataCollection<uint> uintDataCollection)
                {
                    parameterViewModel.Value = _dataFormatter.FormatUInt32(uintDataCollection.Values[index]);
                }
                else if (DataCollections[parameter.Number] is DataCollection<ushort> shortDataCollection)
                {
                    parameterViewModel.Value = _dataFormatter.FormatUInt16(shortDataCollection.Values[index]);
                }
                else if (DataCollections[parameter.Number] is DataCollection<TimeSpan> timeSpanDataCollection)
                {
                    parameterViewModel.Value = _dataFormatter.FormatTimeSpan(timeSpanDataCollection.Values[index]);
                }
                else if (DataCollections[parameter.Number] is DataCollection<DateTime> dateTimeDataCollection)
                {
                    parameterViewModel.Value = _dataFormatter.FormatDateTime(dateTimeDataCollection.Values[index]);
                }
            }
        }
    }
}
