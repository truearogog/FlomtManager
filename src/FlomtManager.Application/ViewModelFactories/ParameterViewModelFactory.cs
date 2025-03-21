using FlomtManager.Application.ViewModels;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.Application.ViewModelFactories;

internal sealed class ParameterViewModelFactory(IServiceProvider serviceProvider) : IParameterViewModelFactory
{
    public IParameterViewModel Create(Parameter parameter, bool editable)
    {
        var parameterStore = serviceProvider.GetRequiredService<IParameterStore>();
        var parameterRepository = serviceProvider.GetRequiredService<IParameterRepository>();

        return new ParameterViewModel(parameter, editable, parameterStore, parameterRepository);
    }
}
