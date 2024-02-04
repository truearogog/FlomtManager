using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FlomtManager.MemoryReader.ViewModels;
using System;

namespace FlomtManager.MemoryReader.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.DirectoryRequested += _DirectoryRequested;
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
    }
}