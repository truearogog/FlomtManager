using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Skia;
using DynamicData;
using FlomtManager.App.Extensions;
using FlomtManager.App.ViewModels;
using FlomtManager.Core.Attributes;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Framework.Extensions;
using ScottPlot;
using ScottPlot.Control;
using ScottPlot.Plottables;
using SkiaSharp;

namespace FlomtManager.App.Views
{
    public partial class DataGroupChart : UserControl
    {
        private HorizontalSpan? _selectionSpan;
        private AxisSpanUnderMouse? _spanBeingDragged;
        private Crosshair? _crosshair;

        // parameter number, plot
        private Dictionary<byte, SignalXY> _charts = [];

        private bool _lockX = true;
        private bool _lockY = false;

        private DataGroupChartViewModel? _viewModel;

        public DataGroupChart()
        {
            InitializeComponent();
            Debug.WriteLine(this);
            ConfigureChart();
            LockY();

            Chart.PointerPressed += Chart_PointerPressed;
            Chart.PointerMoved += Chart_PointerMoved;
            Chart.PointerReleased += Chart_PointerReleased;

            SetChartTheme();
            App.Current!.ActualThemeVariantChanged += _ActualThemeVariantChanged;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is DataGroupChartViewModel viewModel)
            {
                viewModel.OnDataUpdate += _OnDataUpdate;
                viewModel.OnParameterToggled += _OnParameterToggled;
                viewModel.OnYAxesToggled += _OnYAxesToggled;
                viewModel.UpdateData();

                _viewModel = viewModel;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (DataContext is DataGroupChartViewModel viewModel)
            {
                viewModel.OnDataUpdate -= _OnDataUpdate;
                viewModel.OnParameterToggled -= _OnParameterToggled;
                viewModel.OnYAxesToggled -= _OnYAxesToggled;

                _viewModel = null;
            }
        }

        private void _OnDataUpdate(object sender, IEnumerable<DataGroupValues> dataGroups)
        {
            var parameters = dataGroups.FirstOrDefault()?.Parameters;
            if (parameters == null)
            {
                return;
            }

            foreach (var (parameterNumber, chart) in _charts)
            {
                Chart.Plot.Remove(chart);
                _charts.Remove(parameterNumber);
            }

            var current = 0;
            var xs = dataGroups.Select(x => x.DateTime.ToOADate()).ToArray();
            foreach (var parameter in parameters)
            {
                if (parameter.ParameterType.GetAttribute<HideAttribute>()?.Hide(HideTargets.Chart) != true)
                {
                    var ys = dataGroups.Select(x => x.Values[current]).ToArray();
                    var signalXY = Chart.Plot.Add.SignalXY(xs, ys);
                    signalXY.Color = Color.FromSKColor(SKColor.Parse(parameter.Color));
                    signalXY.LineStyle.Width = 2.5f;

                    var yAxis = Chart.Plot.Axes.AddLeftAxis();
                    yAxis.IsVisible = (DataContext as DataGroupChartViewModel)?.YAxesVisible ?? false;
                    yAxis.Label.Text = $"{parameter.Name}, {parameter.Unit}";
                    yAxis.Color(Color.FromSKColor(SKColor.Parse(parameter.Color)));

                    signalXY.Axes.YAxis = yAxis;

                    _charts[parameter.Number] = signalXY;
                }
                current++;
            }

            Chart.Plot.Axes.SetLimitsX(xs.First() - 1, xs.Last() + 1);
            Chart.Plot.Axes.AutoScaleY();
            Chart.Refresh();
        }

        private void _OnParameterToggled(object sender, byte parameterNumber)
        {
            if (_charts.TryGetValue(parameterNumber, out var chart))
            {
                chart.IsVisible = !chart.IsVisible;
                chart.Axes.YAxis!.IsVisible = chart.IsVisible && ((DataContext as DataGroupChartViewModel)?.YAxesVisible ?? false);
                Chart.Refresh();
            }
        }

        private void _OnYAxesToggled(object sender, bool visible)
        {
            foreach (var chart in _charts.Values.Where(x => x.IsVisible))
            {
                chart.Axes.YAxis!.IsVisible = visible;
            }
            Chart.Refresh();
        }

        private void Chart_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(Chart);
            var coordinates = Chart.Plot.GetCoordinates((float)point.Position.X, (float)point.Position.Y);
            var roundedCoordinates = coordinates with { X = RoundADateToHour(coordinates.X) };
            if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            {
                RightButtonPressed();
            }
            if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                if (e.ClickCount == 1)
                {
                    var thingUnderMouse = GetSpanUnderMouse((float)point.Position.X, (float)point.Position.Y);
                    if (thingUnderMouse is not null)
                    {
                        _spanBeingDragged = thingUnderMouse;
                        _crosshair!.IsVisible = false;
                        Chart.Refresh();
                    }
                    else
                    {
                        if (_selectionSpan is not null)
                        {
                            Chart.Plot.Remove(_selectionSpan);
                            _selectionSpan = null;
                        }

                        _selectionSpan = Chart.Plot.Add.HorizontalSpan(roundedCoordinates.X, roundedCoordinates.X);
                        var lineColor = ScottPlot.Color.FromSKColor(Avalonia.Media.Colors.Red.ToSKColor()).WithAlpha(0.7);
                        var fillColor = lineColor.WithAlpha(0.1);
                        _selectionSpan.LineStyle.Color = lineColor;
                        _selectionSpan.FillStyle.Color = fillColor;
                        _selectionSpan.IsDraggable = true;
                        _selectionSpan.IsResizable = true;

                        var axisSpanUnderMouse = GetSpanUnderMouse((float)point.Position.X, (float)point.Position.Y);
                        _spanBeingDragged = axisSpanUnderMouse;
                        _crosshair!.IsVisible = false;
                        Chart.Refresh();
                    }
                }
                else if (e.ClickCount == 2)
                {
                    Chart.Plot.Benchmark.IsVisible = false;
                    if (_selectionSpan is not null)
                    {
                        Chart.Plot.Remove(_selectionSpan);
                        _selectionSpan = null;
                    }
                }
            }
        }

        private void Chart_PointerMoved(object sender, PointerEventArgs e)
        {
            var point = e.GetCurrentPoint(Chart);
            var coordinates = Chart.Plot.GetCoordinates((float)point.Position.X, (float)point.Position.Y);
            var roundedCoordinates = coordinates with { X = RoundADateToHour(coordinates.X) };
            if (_spanBeingDragged is not null)
            {
                _spanBeingDragged.DragTo(roundedCoordinates);
                Cursor = new Cursor(StandardCursorType.SizeWestEast);
                Chart.Refresh();

                if (_viewModel != null && _selectionSpan != null)
                {
                    _viewModel.IntegrationSpanMinDate = _selectionSpan.XRange.Min;
                    _viewModel.IntegrationSpanMaxDate = _selectionSpan.XRange.Max;
                }
            }
            else
            {
                _viewModel!.CurrentDisplayDate = roundedCoordinates.X;
                _crosshair!.Position = roundedCoordinates;
                Chart.Refresh();

                var spanUnderMouse = GetSpanUnderMouse((float)point.Position.X, (float)point.Position.Y);
                if (spanUnderMouse is null)
                {
                    Cursor = Cursor.Default;
                }
                else if (spanUnderMouse.IsResizingHorizontally)
                {
                    Cursor = new Cursor(StandardCursorType.SizeWestEast);
                }
                else if (spanUnderMouse.IsMoving)
                {
                    Cursor = new Cursor(StandardCursorType.SizeAll);
                }
            }
        }

        private static double RoundADateToHour(double adate)
        {
            return double.Round(adate * 24) / 24;
        }

        private void Chart_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var point = e.GetCurrentPoint(Chart);
            if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
            {
                RightButtonReleased();
            }
            if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                _spanBeingDragged = null;
                _crosshair!.IsVisible = true;
                Chart.Refresh();
            }
        }
        

        private void RightButtonPressed()
        {
            LockX();
        }

        private void RightButtonReleased()
        {
            LockY();
        }

        private void ConfigureChart()
        {
            _crosshair = Chart.Plot.Add.Crosshair(0, 0);

            Chart.ContextMenu = null;

            var inputBindings = new InputBindings()
            {
                DragPanButton = ScottPlot.Control.MouseButton.Middle,
                ZoomInWheelDirection = MouseWheelDirection.Up,
                ZoomOutWheelDirection = MouseWheelDirection.Down,
            };

            var interaction = new Interaction(Chart)
            {
                Inputs = inputBindings,
                Actions = new()
                {
                    DragPan = DragPan,
                    ZoomIn = ZoomIn,
                    ZoomOut = ZoomOut,
                },
            };

            Chart.Plot.Axes.Remove(Edge.Top);
            Chart.Plot.Axes.Remove(Edge.Right);
            Chart.Plot.Axes.Remove(Edge.Left);
            Chart.Plot.Axes.DateTimeTicksBottom();
            Chart.Interaction = interaction;
            Chart.Refresh();
        }

        private void LockX()
        {
            _lockX = true;
            _lockY = false;
        }

        private void LockY()
        {
            _lockX = false;
            _lockY = true;
        }

        private void DragPan(IPlotControl control, MouseDrag drag, LockedAxes locked)
        {
            Pixel mouseNow = new(_lockX ? drag.From.X : drag.To.X, _lockY ? drag.From.Y : drag.To.Y);
            control.Plot.MousePan(drag.InitialLimits, drag.From, mouseNow);
            control.Refresh();
        }

        public void ZoomIn(IPlotControl control, Pixel pixel, LockedAxes locked)
        {
            double num = 1.15;
            double fracX = _lockX ? 1.0 : num;
            double fracY = _lockY ? 1.0 : num;
            control.Plot.MouseZoom(fracX, fracY, pixel);
            control.Refresh();
        }

        public void ZoomOut(IPlotControl control, Pixel pixel, LockedAxes locked)
        {
            double num = 0.85;
            double fracX = _lockX ? 1.0 : num;
            double fracY = _lockY ? 1.0 : num;
            control.Plot.MouseZoom(fracX, fracY, pixel);
            control.Refresh();
        }

        private AxisSpanUnderMouse? GetSpanUnderMouse(float x, float y)
        {
            var coordinates = Chart.Plot.GetCoordinates(x, y);
            var roundedCoordinates = coordinates with { X = RoundADateToHour(coordinates.X) };
            var rect = Chart.Plot.GetCoordinateRect(roundedCoordinates, radius: 10);
            foreach (var span in Chart.Plot.GetPlottables<AxisSpan>().Reverse())
            {
                var spanUnderMouse = span.UnderMouse(rect);
                if (spanUnderMouse is not null)
                    return spanUnderMouse;
            }

            return null;
        }

        private void _ActualThemeVariantChanged(object sender, EventArgs e)
        {
            SetChartTheme();
        }

        private void SetChartTheme()
        {
            var themeVariant = App.Current!.ActualThemeVariant;

            var backgroundColor = App.Current!.GetBrushResource("SemiColorBackground0", themeVariant).Color.ToSKColor();
            Chart.Plot.DataBackground = ScottPlot.Color.FromSKColor(backgroundColor);
            Chart.Plot.FigureBackground = ScottPlot.Color.FromSKColor(backgroundColor);

            var axesColor = App.Current!.GetBrushResource("SemiGrey9", themeVariant).Color.ToSKColor();
            Chart.Plot.Axes.Bottom.FrameLineStyle.Color = ScottPlot.Color.FromSKColor(axesColor);
            Chart.Plot.Axes.Bottom.MajorTickStyle.Color = ScottPlot.Color.FromSKColor(axesColor);
            Chart.Plot.Axes.Bottom.MinorTickStyle.Color = ScottPlot.Color.FromSKColor(axesColor);
            Chart.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromSKColor(axesColor);

            var gridColor = App.Current!.GetBrushResource("SemiGrey1", themeVariant).Color.ToSKColor();
            Chart.Plot.Style.ColorGrids(ScottPlot.Color.FromSKColor(gridColor));
            
            var crosshairColor = App.Current!.GetBrushResource("SemiBlue4", themeVariant).Color.ToSKColor();
            _crosshair!.LineStyle.Color = ScottPlot.Color.FromSKColor(crosshairColor);
        }
    }
}