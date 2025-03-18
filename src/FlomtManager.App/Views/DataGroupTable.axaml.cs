using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using FlomtManager.Domain.Abstractions.Parsers;
using FlomtManager.Domain.Abstractions.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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

                Task.Run(viewModel.UpdateData);
                _viewModel = viewModel;
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
                Table.Columns.Clear();

                var bindingFormat = $"{nameof(IDataTableViewModel.ValueCollection.Values)}[{{0}}]";

                foreach (var parameter in _viewModel.Parameters)
                {
                    var header = $"{parameter.Name}" + (string.IsNullOrEmpty(parameter.Unit) ? "" : $", {parameter.Unit}");
                    var index = _viewModel.ParameterPositions[parameter.Number];
                    var format = dataFormatter.GetParameterFormat(parameter);
                    var path = string.Format(bindingFormat, index);

                    Table.Columns.Add(new DataGridTextColumn
                    {
                        Header = header,
                        CanUserSort = true,
                        CanUserReorder = false,
                        Binding = new Binding
                        {
                            Mode = BindingMode.OneWay,
                            Path = path,
                            StringFormat = format,
                        },
                        Width = parameter.Number switch
                        {
                            0 => DataGridLength.Auto,
                            _ => new DataGridLength(1, DataGridLengthUnitType.Star)
                        }
                    });
                }

                var sortDescription = DataGridSortDescription.FromPath(string.Format(bindingFormat, 0), ListSortDirection.Descending);
                var collectionView = new DataGridCollectionView(_viewModel.Data);
                collectionView.SortDescriptions.Add(sortDescription);

                Table.ItemsSource = collectionView;
            });
        }
    }
}