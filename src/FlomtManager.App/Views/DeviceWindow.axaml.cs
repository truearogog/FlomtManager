using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using FlomtManager.App.Extensions;
using FlomtManager.App.Stores;
using FlomtManager.App.ViewModels;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FlomtManager.App.Views
{
    public partial class DeviceWindow : Window
    {
        private WindowNotificationManager _windowNotificationManager;

        public DeviceWindow()
        {
            InitializeComponent();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            var topLevel = GetTopLevel(this);
            _windowNotificationManager = new WindowNotificationManager(topLevel) { MaxItems = 3 };
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            if (DataContext is IDeviceViewModel viewModel)
            {
                var deviceWindowStore = App.Services.GetRequiredService<DeviceWindowStore>();
                deviceWindowStore.RemoveWindow(viewModel.Device.Id);

                viewModel.TryDisconnect().Wait();
                viewModel.SaveRealtimeParameters().Wait();
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is IDeviceViewModel viewModel)
            {
                viewModel.CloseRequested += viewModel_CloseRequested;
                viewModel.DeviceUpdateRequested += viewModel_DeviceUpdateRequested;
                viewModel.ReadFromFileRequested += viewModel_ReadFromFileRequested;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (DataContext is IDeviceViewModel viewModel)
            {
                viewModel.CloseRequested -= viewModel_CloseRequested;
                viewModel.DeviceUpdateRequested -= viewModel_DeviceUpdateRequested;
                viewModel.ReadFromFileRequested -= viewModel_ReadFromFileRequested;
            }
        }

        private void viewModel_CloseRequested(object sender, EventArgs e)
        {
            Close();
        }

        private async void viewModel_DeviceUpdateRequested(object sender, Device device)
        {
            var viewModel = App.Services.GetRequiredService<IDeviceCreateUpdateViewModel>();
            viewModel.SetDevice(device);

            var window = new DeviceCreateUpdateWindow
            {
                DataContext = viewModel
            };
            var parentWindow = this.GetWindow();
            await window.ShowDialog(parentWindow);
        }

        private async void viewModel_ReadFromFileRequested(object sender, EventArgs e)
        {
            if (DataContext is IDeviceViewModel viewModel)
            {
                var sp = GetTopLevel(this)?.StorageProvider;
                if (sp == null)
                {
                    return;
                }
                var result = await sp.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = "Select File",
                    FileTypeFilter = [FlomtFilePickerFileTypes.Hex],
                    AllowMultiple = false,
                });
                if (result.Count == 0)
                {
                    return;
                }
                var file = result[0];
                using var stream = await file.OpenReadAsync();
                await viewModel.ReadFile(stream);
            }
        }
    }
}
