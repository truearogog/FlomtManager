using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.ViewModels;

public interface IParameterViewModel : IViewModel
{
    Parameter Parameter { get; }
    string Header { get; }

    bool IsAxisVisibleOnChart { get; set; }
    bool IsAutoScaledOnChart { get; set; }
    double ZoomLevelOnChart { get; set; }
    string Color { get; set; }

    bool IsEnabled { get; set; }
    string Value { get; set; }
    bool Error { get; set; }
    bool Editable { get; set; }
    bool Toggleable { get; set; }

    Task Toggle();
}
