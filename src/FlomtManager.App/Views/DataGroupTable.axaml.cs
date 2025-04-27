using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using FlomtManager.Domain.Abstractions.ViewModels;

namespace FlomtManager.App.Views
{
    public partial class DataGroupTable : UserControl
    {
        private IDataTableViewModel _viewModel;

        public DataGroupTable()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is IDataTableViewModel viewModel)
            {
                viewModel.OnDataUpdated += _OnDataUpdated;
                viewModel.OnCurrentDisplaySpanChanged += _OnCurrentDisplaySpanChanged;

                _viewModel = viewModel;
                Task.Run(_viewModel.UpdateData);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (DataContext is IDataTableViewModel viewModel)
            {
                viewModel.OnDataUpdated -= _OnDataUpdated;
                viewModel.OnCurrentDisplaySpanChanged -= _OnCurrentDisplaySpanChanged;

                _viewModel = null;
            }
        }

        private void _OnDataUpdated(object sender, EventArgs eventArgs)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (this.FindControl<ItemsControl>("ParameterGrid") is { } parameterGrid)
                {
                    parameterGrid.ItemsSource = _viewModel.Parameters;
                }

                if (this.FindControl<ListBox>("DataGrid") is { } dataGrid)
                {
                    dataGrid.ItemsSource = _viewModel.Data;
                    dataGrid.UpdateLayout();
                }
            });
        }

        private void _OnCurrentDisplaySpanChanged(object sender, (int Min, int Max) e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (this.FindControl<ListBox>("DataGrid") is { } dataGrid)
                {
                    dataGrid.SelectedIndex = 0;
                    dataGrid.SelectedIndex = Math.Clamp(0, dataGrid.ItemCount - e.Min - 1, dataGrid.ItemCount - 1);
                }
            });
        }
    }
}