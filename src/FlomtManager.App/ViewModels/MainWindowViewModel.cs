namespace FlomtManager.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public DevicesViewModel DevicesViewModel { get; set; }

        public MainWindowViewModel(DevicesViewModel devicesViewModel)
        {
            DevicesViewModel = devicesViewModel;
        }
    }
}
