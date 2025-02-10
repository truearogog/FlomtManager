using Avalonia.Controls;
using FlomtManager.App.ViewModels;

namespace FlomtManager.App.Views
{
    public partial class DeviceCreateUpdateWindow : Window
    {
        public DeviceCreateUpdateWindow()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is DeviceCreateUpdateViewModel viewModel)
            {
                viewModel.CloseRequested = _CloseRequested;
            }
        }

        private void _CloseRequested(object sender, EventArgs e)
        {
            Close();
        }
    }
}