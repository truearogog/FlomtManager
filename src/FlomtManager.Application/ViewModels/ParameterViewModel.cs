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

    private bool _yAxisIsVisible;
    public bool YAxisIsVisible
    {
        get => _yAxisIsVisible;
        set => this.RaiseAndSetIfChanged(ref _yAxisIsVisible, value);
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
            this.WhenAnyValue(x => x.YAxisIsVisible)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async x => await UpdateYAxisIsVisible(x));

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
        YAxisIsVisible = parameter.YAxisIsVisible;
        Color = parameter.Color;
    }

    private void _parameterStore_Updated(object sender, Parameter e)
    {
        if (e.Id == Parameter.Id)
        {
            SetParameter(e);
        }
    }

    private async Task UpdateYAxisIsVisible(bool yAxisIsVisible)
    {
        await _parameterRepository.UpdateYAxisIsVisible(Parameter.Id, yAxisIsVisible);
        _parameterStore.Update(Parameter with { YAxisIsVisible = yAxisIsVisible });
    }

    private async Task UpdateColor(string color)
    {
        await _parameterRepository.UpdateColor(Parameter.Id, color);
        _parameterStore.Update(Parameter with { Color = color });
    }
}
