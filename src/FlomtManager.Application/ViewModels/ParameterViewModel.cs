using System.Reactive.Linq;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;
using ReactiveUI;

namespace FlomtManager.Application.ViewModels;

internal sealed class ParameterViewModel : ViewModel, IParameterViewModel
{
    private readonly IParameterStore _parameterStore;
    private readonly IParameterRepository _parameterRepository;

    private Parameter _parameter = default;
    public Parameter Parameter
    {
        get => _parameter;
        private set => this.RaiseAndSetIfChanged(ref _parameter, value);
    }

    public string Header => Parameter.Name + (string.IsNullOrEmpty(Parameter.Unit) ? "" : $", {Parameter.Unit}");

    private bool _isAxisVisibleOnChart;
    public bool IsAxisVisibleOnChart
    {
        get => _isAxisVisibleOnChart;
        set => this.RaiseAndSetIfChanged(ref _isAxisVisibleOnChart, value);
    }

    private bool _isAutoScaledOnChart;
    public bool IsAutoScaledOnChart
    {
        get => _isAutoScaledOnChart;
        set => this.RaiseAndSetIfChanged(ref _isAutoScaledOnChart, value);
    }

    private double _zoomLevelOnChart;
    public double ZoomLevelOnChart
    {
        get => _zoomLevelOnChart;
        set => this.RaiseAndSetIfChanged(ref _zoomLevelOnChart, value);
    }

    private string _color = string.Empty;
    public string Color
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }

    private string _value = string.Empty;
    public string Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    private bool _error = false;
    public bool Error
    {
        get => _error;
        set => this.RaiseAndSetIfChanged(ref _error, value);
    }

    private bool _editable = false;
    public bool Editable
    {
        get => _editable;
        set => this.RaiseAndSetIfChanged(ref _editable, value);
    }

    public ParameterViewModel(Parameter parameter, bool editable, IParameterStore parameterStore, IParameterRepository parameterRepository)
    {
        SetParameter(parameter);
        Editable = editable;

        _parameterStore = parameterStore;
        _parameterStore.Updated += _parameterStore_Updated;
        _parameterRepository = parameterRepository;

        if (editable)
        {
            this.WhenAnyValue(x => x.IsAxisVisibleOnChart)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async x => await UpdateIsAxisVisibleOnChart(x));

            this.WhenAnyValue(x => x.IsAutoScaledOnChart)
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async x => await UpdateIsAutoScaledOnChart(x));

            this.WhenAnyValue(x => x.ZoomLevelOnChart)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async x => await UpdateZoomLevelOnChart(x));

            this.WhenAnyValue(x => x.Color)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async x => await UpdateColor(x));
        }
    }

    private void SetParameter(Parameter parameter)
    {
        Parameter = parameter;
        IsAxisVisibleOnChart = parameter.IsAxisVisibleOnChart;
        IsAutoScaledOnChart = parameter.IsAutoScaledOnChart;
        ZoomLevelOnChart = parameter.ZoomLevelOnChart;
        Color = parameter.Color;
    }

    private void _parameterStore_Updated(object sender, Parameter e)
    {
        if (e.Id == Parameter.Id)
        {
            SetParameter(e);
        }
    }

    private async Task UpdateIsAxisVisibleOnChart(bool isAxisVisibleOnChart)
    {
        await _parameterRepository.UpdateIsAxisVisibleOnChart(Parameter.Id, isAxisVisibleOnChart);
        _parameterStore.Update(Parameter with { IsAxisVisibleOnChart = isAxisVisibleOnChart });
    }

    private async Task UpdateIsAutoScaledOnChart(bool isAutoScaledOnChart)
    {
        await _parameterRepository.UpdateIsAutoScaledOnChart(Parameter.Id, isAutoScaledOnChart);
        _parameterStore.Update(Parameter with { IsAutoScaledOnChart = isAutoScaledOnChart });
    }

    private async Task UpdateZoomLevelOnChart(double zoomLevelOnChart)
    {
        await _parameterRepository.UpdateZoomLevelOnChart(Parameter.Id, zoomLevelOnChart);
        _parameterStore.Update(Parameter with { ZoomLevelOnChart = zoomLevelOnChart });
    }

    private async Task UpdateColor(string color)
    {
        await _parameterRepository.UpdateColor(Parameter.Id, color);
        _parameterStore.Update(Parameter with { Color = color });
    }
}
