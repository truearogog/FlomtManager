using Avalonia.Controls;
using FlomtManager.Domain.Abstractions.ViewModels;

namespace FlomtManager.App.Views;

public partial class DeviceDeleteWindow : Window
{
    public DeviceDeleteWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is IDeviceDeleteViewModel viewModel)
        {
            viewModel.CloseRequested += viewModel_CloseRequested;
        }
    }

    private void viewModel_CloseRequested(object sender, EventArgs e)
    {
        Close();
    }
}