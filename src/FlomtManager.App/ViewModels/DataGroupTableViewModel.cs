using FlomtManager.Core.Models;
using FlomtManager.Core.Services;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace FlomtManager.App.ViewModels
{
    public class DataGroupTableViewModel(IDataService dataService) : ViewModelBase
    {
        private readonly IDataService _dataService = dataService;

        private Device? _device;
        public Device? Device
        {
            get => _device;
            set => this.RaiseAndSetIfChanged(ref _device, value);
        }

        public ObservableCollection<DataGroupValues> Data { get; set; } = [];
        public event EventHandler<IEnumerable<DataGroupValues>>? OnDataUpdate;

        public async void UpdateData()
        {
            var dataGroups = await _dataService.GetDataGroupValues(Device?.Id ?? 0);
            if (dataGroups.Length != 0)
            {
                Data = new(dataGroups);
                OnDataUpdate?.Invoke(this, dataGroups);
            }
        }
    }
}
