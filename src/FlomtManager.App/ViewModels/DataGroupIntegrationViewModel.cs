using System.Collections.ObjectModel;
using FlomtManager.App.Models;
using FlomtManager.Core.Events;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using ReactiveUI;

namespace FlomtManager.App.ViewModels
{
    public class DataGroupIntegrationViewModel(IParameterRepository parameterRepository) : ViewModel
    {
        private readonly IParameterRepository _parameterRepository = parameterRepository;

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

        public ObservableCollection<ParameterViewModel> Parameters { get; set; } = [];

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
            var integrationParameters = await _parameterRepository.GetIntegralParametersByDeviceId(Device.Id);
            foreach (var integrationParameter in integrationParameters)
            {
                Parameters.Add(new() { Parameter = integrationParameter });
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
}
