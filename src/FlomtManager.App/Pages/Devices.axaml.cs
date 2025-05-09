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
                viewModel.DeviceDeleteRequested += viewModel_DeviceDeleteRequested;
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
                viewModel.DeviceDeleteRequested -= viewModel_DeviceDeleteRequested;
                viewModel.DeviceViewRequested -= viewModel_DeviceViewRequested;
            }
        }

        private async void viewModel_DeviceCreateRequested(object sender, EventArgs e)
        {
            var viewModel = App.Services.GetRequiredService<IDeviceCreateUpdateViewModel>();
            await viewModel.ActivateCreate();

            var window = new DeviceCreateUpdateWindow
            {
                DataContext = viewModel
            };
            var parentWindow = this.GetWindow();
            await window.ShowDialog(parentWindow);
        }

        private async void viewModel_DeviceUpdateRequested(object sender, Device device)
        {
            var viewModel = App.Services.GetRequiredService<IDeviceCreateUpdateViewModel>();
            viewModel.ActivateUpdate(device);

            var window = new DeviceCreateUpdateWindow
            {
                DataContext = viewModel
            };
            var parentWindow = this.GetWindow();
            await window.ShowDialog(parentWindow);
        }

        private async void viewModel_DeviceDeleteRequested(object sender, Device device)
        {
            var viewModel = App.Services.GetRequiredService<IDeviceDeleteViewModel>();
            viewModel.Activate(device);

            var window = new DeviceDeleteWindow
            {
                DataContext = viewModel
            };
            var parentWindow = this.GetWindow();
            await window.ShowDialog(parentWindow);
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
            await viewModel.Activate(device);

            var newWindow = new DeviceWindow
            {
                DataContext = viewModel
            };
            deviceWindowStore.AddWindow(device.Id, newWindow);
            newWindow.Show();
        }
    }
}
