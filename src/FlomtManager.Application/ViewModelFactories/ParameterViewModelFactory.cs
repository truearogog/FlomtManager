using FlomtManager.Application.ViewModels;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Abstractions.ViewModelFactories;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.Application.ViewModelFactories;

internal sealed class ParameterViewModelFactory(IServiceProvider serviceProvider) : IParameterViewModelFactory
{
    public IParameterViewModel Create(Parameter parameter)
    {
        var parameterRepository = serviceProvider.GetRequiredService<IParameterRepository>();

        return new ParameterViewModel(parameter, parameterRepository);
    }
}
