using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IParameterViewModel : IViewModel
{
    Parameter Parameter { get; }
    bool YAxisIsVisible { get; set; }
    string Color { get; set; }

    string Value { get; set; }
    bool Error { get; set; }
    bool Editable { get; set; }
}
