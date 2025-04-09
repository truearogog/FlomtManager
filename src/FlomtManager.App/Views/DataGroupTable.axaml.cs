using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using FlomtManager.Domain.Abstractions.Parsers;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models.Collections;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

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

                _viewModel = null;
            }
        }

        private void _OnDataUpdated(object sender, EventArgs eventArgs)
        {
            var dataFormatter = App.Services.GetRequiredService<IDataFormatter>();

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
    }
}