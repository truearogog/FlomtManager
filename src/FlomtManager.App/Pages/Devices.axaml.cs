using Avalonia.Controls;
using FlomtManager.Core.Models;
using FlomtManager.App.Extensions;
using FlomtManager.App.ViewModels;
using FlomtManager.App.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

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
                viewModel.DeviceCreateRequested = _DeviceCreateRequested;
                viewModel.DeviceUpdateRequested = _DeviceUpdateRequested;
                viewModel.DeviceViewRequested = _DeviceViewRequested;
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
            var viewModel = App.Host.Services.GetRequiredService<DeviceViewModel>();
            viewModel.Device = device;

            var window = new DeviceWindow
            {
                DataContext = viewModel
            };
            window.Show();
        }
    }
}
