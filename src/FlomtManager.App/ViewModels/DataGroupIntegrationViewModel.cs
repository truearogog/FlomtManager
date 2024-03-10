using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace FlomtManager.App.ViewModels
{
    public class DataGroupIntegrationViewModel(IDeviceDefinitionRepository deviceDefinitionRepository, IParameterRepository parameterRepository) : ViewModelBase
    {
        private readonly IDeviceDefinitionRepository _deviceDefinitionRepository = deviceDefinitionRepository;
        private readonly IParameterRepository _parameterRepository = parameterRepository;

        private Device? _device;
        public Device? Device
        {
            get => _device;
            set
            {
                this.RaiseAndSetIfChanged(ref _device, value);
                AddParameters();
            }
        }

        public ObservableCollection<ParameterViewModel> Parameters { get; set; } = [];

        private async void AddParameters()
        {
            ArgumentNullException.ThrowIfNull(Device);
            var deviceDefinition = await _deviceDefinitionRepository.GetById(Device.Id);
            ArgumentNullException.ThrowIfNull(deviceDefinition);
            Parameters.Clear();
            var parameters = await _parameterRepository.GetAll(x => x.DeviceId == Device.Id);
            foreach (var parameterByte in deviceDefinition.AverageParameterArchiveLineDefinition!)
            {
                var parameter = parameters.FirstOrDefault(x => x.Number == parameterByte);
                if (parameter != null && parameter.IntegrationNumber != 0)
                {
                    var integrationParameter = parameters.FirstOrDefault(x => x.Number == parameter.IntegrationNumber);
                    if (integrationParameter != null)
                    {
                        Parameters.Add(new() { Parameter = integrationParameter });
                    }
                }
            }
        }

        public void UpdateValues(IEnumerable<(byte, float)> integration)
        {
            foreach (var (number, value) in integration)
            {
                var parameter = Parameters.FirstOrDefault(x => x.Parameter.Number == number);
                if (parameter != null)
                {
                    parameter.Value = value.ToString("0.###");
                }
            }
        }
    }
}
