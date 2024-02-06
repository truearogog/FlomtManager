namespace FlomtManager.App.ViewModels
{
    public class MainWindowViewModel(DevicesViewModel devicesViewModel) : ViewModelBase
    {
        public DevicesViewModel DevicesViewModel { get; set; } = devicesViewModel;
    }
}
