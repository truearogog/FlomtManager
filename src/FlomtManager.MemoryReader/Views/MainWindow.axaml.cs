using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using FlomtManager.MemoryReader.ViewModels;

namespace FlomtManager.MemoryReader.Views
{
    public partial class MainWindow : Window
    {
        private WindowNotificationManager? _windowNotificationManager;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            var topLevel = GetTopLevel(this);
            _windowNotificationManager = new WindowNotificationManager(topLevel) { MaxItems = 1 };
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.DirectoryRequested += _DirectoryRequested;
                viewModel.NotificationRequested += _NotificationRequested;
            }
        }

        private async void _DirectoryRequested(object? sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.Form.Directory = null;
                var sp = GetTopLevel(this)?.StorageProvider;
                if (sp == null)
                {
                    return;
                }
                var result = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions()
                {
                    Title = "Select Directory",
                    AllowMultiple = false,
                });
                if (result.Count == 0)
                {
                    return;
                }
                var directory = result[0];
                if (directory != null)
                {
                    viewModel.Form.Directory = directory.Path.AbsolutePath;
                }
            }
        }

        private void _NotificationRequested(object? sender, (NotificationType type, string message) notification)
        {
            _windowNotificationManager?.Show(new Notification(notification.type.ToString(), notification.message, notification.type));
        }
    }
}