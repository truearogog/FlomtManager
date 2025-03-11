namespace FlomtManager.App.ViewModels
{
    public class MainWindowViewModel(DevicesViewModel devicesViewModel) : ViewModel
    {
        public DevicesViewModel DevicesViewModel { get; init; } = devicesViewModel;
    }
}
