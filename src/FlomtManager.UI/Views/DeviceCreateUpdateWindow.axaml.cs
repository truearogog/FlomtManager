using Avalonia.Controls;
using FlomtManager.UI.ViewModels;
using System;

namespace FlomtManager.UI.Views
{
    public partial class DeviceCreateUpdateWindow : Window
    {
        public DeviceCreateUpdateWindow()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is DeviceCreateUpdateViewModel viewModel)
            {
                viewModel.CancelRequested = _CancelRequested;
            }
        }

        private void _CancelRequested(object sender, EventArgs e)
        {
            Close();
        }
    }
}