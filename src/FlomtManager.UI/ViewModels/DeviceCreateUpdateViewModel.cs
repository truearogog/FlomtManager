using AutoMapper;
using FlomtManager.Core.Models;
using FlomtManager.Core.Services;
using FlomtManager.Framework.Helpers;
using FlomtManager.UI.Stores;
using ReactiveUI;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FlomtManager.UI.ViewModels
{
    public class DeviceCreateUpdateViewModel : ViewModelBase
    {
        private readonly IDeviceService _deviceService;
        private readonly DeviceStore _deviceStore;
        private readonly IMapper _mapper;

        public DeviceFormViewModel Form { get; set; } = new();

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage; 
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public EventHandler CancelRequested;
        public ICommand CreateDeviceCommand { get; set; }
        public ICommand UpdateDeviceCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public DeviceCreateUpdateViewModel(IDeviceService deviceService, DeviceStore deviceStore, IMapper mapper)
        {
            _deviceService = deviceService;
            _deviceStore = deviceStore;
            _mapper = mapper;

            CreateDeviceCommand = ReactiveCommand.CreateFromTask(CreateDevice);
            UpdateDeviceCommand = ReactiveCommand.CreateFromTask(UpdateDevice);
            CancelCommand = ReactiveCommand.Create(Cancel);
        }

        private void Cancel()
        {
            CancelRequested.Invoke(this, EventArgs.Empty);
        }

        private async Task CreateDevice()
        {
            var validationErrors = ValidationHelper.Validate(Form);
            if (validationErrors.Any())
            {
                return;
            }

            var model = _mapper.Map<Device>(Form);
            var result = await _deviceStore.CreateDevice(_deviceService, model);

            if (result)
            {
                Cancel();
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Couldn't add new device.";
            }
        }

        private async Task UpdateDevice()
        {
            var validationErrors = ValidationHelper.Validate(Form);
            if (validationErrors.Any())
            {
                return;
            }

            var model = _mapper.Map<Device>(Form);
            var result = await _deviceStore.UpdateDevice(_deviceService, model);

            if (result)
            {
                Cancel();
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Couldn't edit device.";
            }
        }

        public void SetDevice(Device device)
        {
            Form = _mapper.Map<DeviceFormViewModel>(device);
        }
    }
}
