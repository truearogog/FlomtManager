﻿using ReactiveUI;

namespace FlomtManager.App.ViewModels
{
    public class ParameterViewModel : ViewModelBase
    {
        public required byte Number { get; set; }
        public required string Name { get; init; }
        public required string Unit { get; init; }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }
    }
}