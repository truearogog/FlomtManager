using Avalonia.Controls;
using FlomtManager.Domain.Abstractions.ViewModels;

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

            if (DataContext is IDeviceCreateUpdateViewModel viewModel)
            {
                viewModel.CloseRequested += viewModel_CloseRequested;
            }
        }

        private void viewModel_CloseRequested(object sender, EventArgs e)
        {
            Close();
        }
    }
}