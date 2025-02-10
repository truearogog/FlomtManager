using System.Collections.ObjectModel;
using FlomtManager.App.Models;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;

namespace FlomtManager.App.ViewModels
{
    public class DataGroupIntegrationViewModel(IDeviceDefinitionRepository deviceDefinitionRepository, IParameterRepository parameterRepository) : ViewModelBase
    {
        private readonly IDeviceDefinitionRepository _deviceDefinitionRepository = deviceDefinitionRepository;
        private readonly IParameterRepository _parameterRepository = parameterRepository;

        private Device _device;
        public Device Device
        {
            get => _device;
            set
            {
                this.RaiseAndSetIfChanged(ref _device, value);
                AddParameters();
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

        private async void AddParameters()
        {
            ArgumentNullException.ThrowIfNull(Device);
            var deviceDefinition = await _deviceDefinitionRepository.GetByIdAsync(Device.Id);
            if (deviceDefinition != null)
            {
                Parameters.Clear();
                var parameters = await _parameterRepository.GetAll().Where(x => x.DeviceId == Device.Id).ToListAsync();
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
        }

        public void UpdateValues(IntegrationChangedEventArgs args)
        {
            IntegrationStart = args.IntegrationStart;
            IntegrationEnd = args.IntegrationEnd;
            foreach (var (number, value) in args.IntegrationData)
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
