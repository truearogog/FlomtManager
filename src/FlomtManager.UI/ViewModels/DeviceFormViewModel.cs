using ReactiveUI;
using System.ComponentModel.DataAnnotations;
using System;

namespace FlomtManager.UI.ViewModels
{
    public class DeviceFormViewModel : ViewModelBase
    {
        private int _id = 0;
        public int Id
        {
            get => _id;
            set => this.RaiseAndSetIfChanged(ref _id, value);
        }

        private string _serialCode;
        [Required]
        public string SerialCode
        {
            get => _serialCode;
            set => this.RaiseAndSetIfChanged(ref _serialCode, value);
        }

        private string _name;
        [Required]
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private string _address;
        public string Address
        {
            get => _address;
            set => this.RaiseAndSetIfChanged(ref _address, value);
        }

        private string _meterNr;
        [Required]
        public string MeterNr
        {
            get => _meterNr;
            set => this.RaiseAndSetIfChanged(ref _meterNr, value);
        }

        public DateTime Created { get; set; }
    }
}
