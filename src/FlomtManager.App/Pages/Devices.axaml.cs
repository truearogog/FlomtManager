using Avalonia.Controls;
using FlomtManager.App.Extensions;
using FlomtManager.App.Stores;
using FlomtManager.App.ViewModels;
using FlomtManager.App.Views;
using FlomtManager.Core.Models;
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

            if (DataContext is DevicesViewModel viewModel)
            {
                viewModel.DeviceCreateRequested += _DeviceCreateRequested;
                viewModel.DeviceUpdateRequested += _DeviceUpdateRequested;
                viewModel.DeviceViewRequested += _DeviceViewRequested;
            }
        }

        private void _DeviceCreateRequested(object? sender, EventArgs e)
        {
            var viewModel = App.Host.Services.GetRequiredService<DeviceCreateUpdateViewModel>();

            var window = new DeviceCreateUpdateWindow
            {
                DataContext = viewModel
            };
            var parentWindow = this.GetWindow();
            window.ShowDialog(parentWindow);
        }

        private void _DeviceUpdateRequested(object? sender, Device device)
        {
            var viewModel = App.Host.Services.GetRequiredService<DeviceCreateUpdateViewModel>();
            viewModel.SetDevice(device);

            var window = new DeviceCreateUpdateWindow
            {
                DataContext = viewModel
            };
            var parentWindow = this.GetWindow();
            window.ShowDialog(parentWindow);
        }

        private void _DeviceViewRequested(object? sender, Device device)
        {
            var deviceWindowStore = App.Host.Services.GetRequiredService<DeviceWindowStore>();
            if (deviceWindowStore.TryGetWindow(device.Id, out var window))
            {
                window!.Activate();
                return;
            }

            var viewModel = App.Host.Services.GetRequiredService<DeviceViewModel>();
            viewModel.Device = device;

            var newWindow = new DeviceWindow
            {
                DataContext = viewModel
            };
            deviceWindowStore.AddWindow(device.Id, newWindow);
            newWindow.Show();
        }
    }
}
