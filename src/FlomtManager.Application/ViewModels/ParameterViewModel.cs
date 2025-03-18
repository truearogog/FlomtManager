using System.Reactive.Linq;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;
using ReactiveUI;

namespace FlomtManager.Application.ViewModels;

internal sealed class ParameterViewModel : ViewModel, IParameterViewModel
{
    private readonly IParameterRepository _parameterRepository;

    public Parameter Parameter { get; init; }

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

    private bool _showYAxis;
    public bool ShowYAxis
    {
        get => _showYAxis;
        set => this.RaiseAndSetIfChanged(ref _showYAxis, value);
    }

    public ParameterViewModel(Parameter parameter, IParameterRepository parameterRepository)
    {
        _parameterRepository = parameterRepository;
        Parameter = parameter;
        ShowYAxis = parameter.ShowYAxis;

        this.WhenAnyValue(x => x.ShowYAxis)
            .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async x => await UpdateShowYAxis(x));
    }

    private async Task UpdateShowYAxis(bool show)
    {
        await _parameterRepository.UpdateShowYAxis(Parameter.Id, show);
    }
}
