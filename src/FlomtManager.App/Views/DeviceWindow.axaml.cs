using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using FlomtManager.App.Extensions;
using FlomtManager.App.Stores;
using FlomtManager.App.ViewModels;
using FlomtManager.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FlomtManager.App.Views
{
    public partial class DeviceWindow : Window
    {
        private WindowNotificationManager? _windowNotificationManager;

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
                deviceWindowStore.RemoveWindow(viewModel.Device!.Id);
                viewModel.TryDisconnect();
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
            }
        }

        private void _CloseRequested(object? sender, EventArgs e)
        {
            Close();
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

        private void _NotificationRequested(object? sender, (NotificationType type, string message) notification)
        {
            _windowNotificationManager?.Show(new Notification(notification.type.ToString(), notification.message, notification.type));
        }
    }
}
