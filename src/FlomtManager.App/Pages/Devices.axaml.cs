using Avalonia;
using Avalonia.Controls;
using FlomtManager.App.Extensions;
using FlomtManager.App.Stores;
using FlomtManager.App.Views;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.App.Pages
{
    public partial class Devices : UserControl
    {
        public Devices()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is IDevicesViewModel viewModel)
            {
                viewModel.DeviceCreateRequested += viewModel_DeviceCreateRequested;
                viewModel.DeviceUpdateRequested += viewModel_DeviceUpdateRequested;
                viewModel.DeviceViewRequested += viewModel_DeviceViewRequested;

                viewModel.LoadDevices();
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (DataContext is IDevicesViewModel viewModel)
            {
                viewModel.DeviceCreateRequested -= viewModel_DeviceCreateRequested;
                viewModel.DeviceUpdateRequested -= viewModel_DeviceUpdateRequested;
                viewModel.DeviceViewRequested -= viewModel_DeviceViewRequested;
            }
        }

        private void viewModel_DeviceCreateRequested(object sender, EventArgs e)
        {
            var viewModel = App.Services.GetRequiredService<IDeviceCreateUpdateViewModel>();

            var window = new DeviceCreateUpdateWindow
            {
                DataContext = viewModel
            };
            var parentWindow = this.GetWindow();
            window.ShowDialog(parentWindow);
        }

        private void viewModel_DeviceUpdateRequested(object sender, Device device)
        {
            var viewModel = App.Services.GetRequiredService<IDeviceCreateUpdateViewModel>();
            viewModel.SetDevice(device);

            var window = new DeviceCreateUpdateWindow
            {
                DataContext = viewModel
            };
            var parentWindow = this.GetWindow();
            window.ShowDialog(parentWindow);
        }

        private async void viewModel_DeviceViewRequested(object sender, Device device)
        {
            var deviceWindowStore = App.Services.GetRequiredService<DeviceWindowStore>();
            if (deviceWindowStore.TryGetWindow(device.Id, out var window))
            {
                window!.Activate();
                return;
            }

            var viewModel = App.Services.GetRequiredService<IDeviceViewModel>();
            await viewModel.SetDevice(device);

            var newWindow = new DeviceWindow
            {
                DataContext = viewModel
            };
            deviceWindowStore.AddWindow(device.Id, newWindow);
            newWindow.Show();
        }
    }
}
