using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using FlomtManager.App.ViewModels;
using FlomtManager.Core.Attributes;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Framework.Extensions;

namespace FlomtManager.App.Views
{
    public partial class DataGroupTable : UserControl
    {
        public DataGroupTable()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is DataGroupTableViewModel viewModel)
            {
                viewModel.OnDataUpdate += _OnDataUpdate;
                viewModel.UpdateData();
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (DataContext is DataGroupTableViewModel viewModel)
            {
                viewModel.OnDataUpdate -= _OnDataUpdate;
            }
        }

        private void _OnDataUpdate(object? sender, IEnumerable<DataGroupValues> dataGroups)
        {
            var parameters = dataGroups.FirstOrDefault()?.Parameters;
            if (parameters == null)
            {
                return;
            }

            Table.Columns.Clear();
            Table.Columns.Add(new DataGridTextColumn
            {
                Header = "Time",
                CanUserSort = true,
                CanUserReorder = false,
                Binding = new Binding()
                {
                    Path = nameof(DataGroupValues.DateTime),
                    Mode = BindingMode.OneWay,
                },
                Width = DataGridLength.Auto
            });

            var current = 0;
            foreach (var parameter in parameters)
            {
                if (parameter.ParameterType.GetAttribute<HideAttribute>()?.Hide(HideTargets.Table) != true)
                {
                    Table.Columns.Add(new DataGridTextColumn
                    {
                        Header = parameter.Name,
                        CanUserSort = true,
                        CanUserReorder = true,
                        Binding = new Binding()
                        {
                            Path = $"{nameof(DataGroupValues.Values)}[{current}]",
                            StringFormat = "{0}",
                            Mode = BindingMode.OneWay,
                        },
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                    });
                }
                current++;
            }
        }
    }
}