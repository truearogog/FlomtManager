using System.Collections.ObjectModel;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Models;
using FlomtManager.Core.Services;
using ReactiveUI;

namespace FlomtManager.App.ViewModels
{
    public class DataGroupTableViewModel(IDataService dataService) : ViewModelBase
    {
        private Device _device;
        public Device Device
        {
            get => _device;
            set => this.RaiseAndSetIfChanged(ref _device, value);
        }

        public ObservableCollection<DataGroupValues> Data { get; set; } = [];
        public event EventHandler<IEnumerable<DataGroupValues>>? OnDataUpdate;

        public async void UpdateData()
        {
            var dataGroups = await dataService.GetDataGroupValues(Device?.Id ?? 0, true);
            if (dataGroups.Length != 0)
            {
                Data = new(dataGroups);
                OnDataUpdate?.Invoke(this, dataGroups);
            }
        }
    }
}
