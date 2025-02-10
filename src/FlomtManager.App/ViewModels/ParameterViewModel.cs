using FlomtManager.Core.Entities;
using ReactiveUI;

namespace FlomtManager.App.ViewModels
{
    public class ParameterViewModel : ViewModelBase
    {
        public required Parameter Parameter { get; init; }

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
    }
}
