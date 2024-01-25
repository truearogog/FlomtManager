using Avalonia;
using Avalonia.Controls;
using FlomtManager.Core.Models;
using FlomtManager.UI.ViewModels;
using FlomtManager.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FlomtManager.UI.Pages
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
            }
        }

        private void _DeviceCreateRequested(object sender, EventArgs e)
        {
            var viewModel = App.Host.Services.GetRequiredService<DeviceCreateUpdateViewModel>();

            var window = new DeviceCreateUpdateWindow
            {
                DataContext = viewModel
            };
            var parentWindow = GetWindow(this);
            window.ShowDialog(parentWindow);
        }

        private void _DeviceUpdateRequested(object sender, Device device)
        {
            var viewModel = App.Host.Services.GetRequiredService<DeviceCreateUpdateViewModel>();
            viewModel.SetDevice(device);

            var window = new DeviceCreateUpdateWindow
            {
                DataContext = viewModel
            };
            var parentWindow = GetWindow(this);
            window.ShowDialog(parentWindow);
        }

        private Window GetWindow(StyledElement element)
        {
            if (element.GetType().IsAssignableTo(typeof(Window)))
            {
                return (Window)element;
            }

            return GetWindow(element.Parent);
        }
    }
}
