using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using FlomtManager.App.Extensions;
using FlomtManager.App.Stores;
using FlomtManager.App.ViewModels;
using FlomtManager.Core.Models;
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
            if (DataContext is DeviceViewModel viewModel)
            {
                var deviceWindowStore = App.Host.Services.GetRequiredService<DeviceWindowStore>();
                deviceWindowStore.RemoveWindow(viewModel.Device.Id);

                viewModel.TryDisconnect().Wait();
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is DeviceViewModel viewModel)
            {
                viewModel.CloseRequested += _CloseRequested;
                viewModel.DeviceUpdateRequested += _DeviceUpdateRequested;
                viewModel.NotificationRequested += _NotificationRequested;
                viewModel.ReadFromFileRequested += _ReadFromFileRequested;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (DataContext is DeviceViewModel viewModel)
            {
                viewModel.CloseRequested -= _CloseRequested;
                viewModel.DeviceUpdateRequested -= _DeviceUpdateRequested;
                viewModel.NotificationRequested -= _NotificationRequested;
                viewModel.ReadFromFileRequested -= _ReadFromFileRequested;
            }
        }

        private void _CloseRequested(object sender, EventArgs e)
        {
            Close();
        }

        private void _DeviceUpdateRequested(object sender, Device device)
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

        private void _NotificationRequested(object sender, (NotificationType type, string message) notification)
        {
            _windowNotificationManager?.Show(new Notification(notification.type.ToString(), notification.message, notification.type));
        }

        private async void _ReadFromFileRequested(object sender, EventArgs e)
        {
            if (DataContext is DeviceViewModel viewModel)
            {
                var sp = GetTopLevel(this)?.StorageProvider;
                if (sp == null)
                {
                    return;
                }
                var result = await sp.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = "Select File",
                    FileTypeFilter = new[] { FlomtFilePickerFileTypes.Hex },
                    AllowMultiple = false,
                });
                if (result.Count == 0)
                {
                    return;
                }
                var file = result[0];
                //await viewModel.ReadArchivesFromFile(file);
            }
        }
    }
}
