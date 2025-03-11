using FlomtManager.Core.Models;
using ReactiveUI;

namespace FlomtManager.App.ViewModels
{
    public class DataGroupTableViewModel : ViewModel
    {
        private Device? _device;
        public Device? Device
        {
            get => _device;
            set => this.RaiseAndSetIfChanged(ref _device, value);
        }

        public Task SetDevice(Device device)
        {
            Device = device;
            return Task.CompletedTask;
        }

        //public ObservableCollection<DataGroupValues> Data { get; set; } = [];
        //public event EventHandler<IEnumerable<DataGroupValues>> OnDataUpdate;

        //public async void UpdateData()
        //{
        //    var dataGroups = await dataService.GetDataGroupValues(Device?.Id ?? 0, true);
        //    if (dataGroups.Length != 0)
        //    {
        //        Data = new(dataGroups);
        //        OnDataUpdate?.Invoke(this, dataGroups);
        //    }
        //}
    }
}
