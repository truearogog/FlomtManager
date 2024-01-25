namespace FlomtManager.UI.ViewModels
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
