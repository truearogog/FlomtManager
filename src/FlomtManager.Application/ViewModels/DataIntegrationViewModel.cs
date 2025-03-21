using FlomtManager.Domain.Abstractions.Repositories;
using System.Collections.ObjectModel;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Abstractions.ViewModels.Events;
using FlomtManager.Domain.Models;
using ReactiveUI;
using FlomtManager.Domain.Abstractions.ViewModelFactories;

namespace FlomtManager.Application.ViewModels;

internal sealed class DataIntegrationViewModel(
    IParameterRepository parameterRepository,
    IParameterViewModelFactory parameterViewModelFactory) : ViewModel, IDataIntegrationViewModel
{
    private Device _device;
    public Device Device
    {
        get => _device;
        set
        {
            this.RaiseAndSetIfChanged(ref _device, value);
        }
    }

    private DateTime? _integrationStart = null;
    public DateTime? IntegrationStart
    {
        get => _integrationStart;
        set => this.RaiseAndSetIfChanged(ref _integrationStart, value);
    }

    private DateTime? _integrationEnd = null;
    public DateTime? IntegrationEnd
    {
        get => _integrationEnd;
        set => this.RaiseAndSetIfChanged(ref _integrationEnd, value);
    }

    public ObservableCollection<IParameterViewModel> Parameters { get; set; } = [];

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
        var currentParameters = await parameterRepository.GetCurrentParametersByDeviceId(Device.Id, true);
        var integrationParameters = await parameterRepository.GetIntegralParametersByDeviceId(Device.Id);
        foreach (var integrationParameter in integrationParameters.Where(x => currentParameters.Any(p => p.IntegrationNumber == x.Number)))
        {
            var parameterViewModel = parameterViewModelFactory.Create(integrationParameter, false);
            Parameters.Add(parameterViewModel);
        }
    }

    public void UpdateValues(IntegrationChangedArgs args)
    {
        IntegrationStart = args.IntegrationStart;
        IntegrationEnd = args.IntegrationEnd;
        foreach (var (number, value) in args.IntegratedValues)
        {
            var parameter = Parameters.FirstOrDefault(x => x.Parameter.Number == number);
            if (parameter != null)
            {
                parameter.Value = value;
            }
        }
    }
}
