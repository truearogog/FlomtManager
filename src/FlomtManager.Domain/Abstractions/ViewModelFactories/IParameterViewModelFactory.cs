using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModelFactories;

public interface IParameterViewModelFactory
{
    IParameterViewModel Create(Parameter parameter, bool editable = false, bool toggleable = false);
}