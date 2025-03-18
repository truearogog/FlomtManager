using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IParameterViewModel : IViewModel
{
    Parameter Parameter { get; init; }
    string Value { get; set; }
    bool Error { get; set; }
    bool ShowYAxis { get; set; }
}
