using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Skia;
using DynamicData;
using FlomtManager.App.Extensions;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Models;
using FlomtManager.Domain.Models.Collections;
using FlomtManager.Framework.Helpers;
using ScottPlot;
using ScottPlot.AxisPanels;
using ScottPlot.Control;
using ScottPlot.Plottables;
using SkiaSharp;

namespace FlomtManager.App.Views
{
    public partial class DataGroupChart : UserControl
    {
        private const double ChartMarginMultiplier = .05;
        private const double ZoomInFactor = 1.2;
        private const double ZoomOutFactor = 1 / ZoomInFactor;

        private HorizontalSpan _selectionSpan;
        private AxisSpanUnderMouse _spanBeingDragged;
        private double _spanLeftOffset, _spanRightOffset;
        private Crosshair _crosshair;

        private readonly Dictionary<byte, ChartInfo> _chartInfos = [];

        private bool _lockX = true;
        private bool _lockY = false;
        private double _yAxisZoom = 1.0d;

        private double _minX = double.NegativeInfinity;
        private double _maxX = double.PositiveInfinity;

        private IDataChartViewModel _viewModel;

        public DataGroupChart()
        {
            InitializeComponent();
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

            if (DataContext is IDataChartViewModel viewModel)
            {
                viewModel.OnParameterUpdated += _OnParameterUpdated;
                viewModel.OnDataUpdated += _OnDataUpdated;

                Task.Run(viewModel.UpdateData);
                _viewModel = viewModel;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (DataContext is IDataChartViewModel viewModel)
            {
                viewModel.OnParameterUpdated -= _OnParameterUpdated;
                viewModel.OnDataUpdated -= _OnDataUpdated;

                _viewModel = null;
            }
        }

        private void _OnParameterUpdated(object sender, Parameter e)
        {
            if (_chartInfos.TryGetValue(e.Number, out var info))
            {
                var color = Color.FromSKColor(SKColor.Parse(e.Color));
                var chart = info.Chart;
                chart.Color = color;
                (chart.Axes.YAxis as LeftAxis)?.Color(color);
                chart.Axes.YAxis.Label.Text = $"{e.Name}, {e.Unit}";

                chart.IsVisible = e.IsEnabled;
                chart.Axes.YAxis.IsVisible = e.IsEnabled && e.IsAxisVisibleOnChart;

                if (e.IsAutoScaledOnChart)
                {
                    // apply auto scaling
                    info.SetYZoom(_yAxisZoom);
                }
                else
                {
                    // apply manual scaling
                    var zoom = Math.Pow(ZoomInFactor, e.ZoomLevelOnChart);
                    info.SetYZoom(zoom);
                }

                info.Parameter = e;
                Chart.Refresh();
            }
        }

        private void _OnDataUpdated(object sender, EventArgs eventArgs)
        {
            // clean charts
            foreach (var chartInfo in _chartInfos.Values)
            {
                Chart.Plot.Remove(chartInfo.Chart);
                _chartInfos.Remove(chartInfo.Parameter.Number);
            }

            if (_viewModel.DateTimes.Length == 0)
            {
                Chart.Refresh();
                return;
            }

            foreach (var parameter in _viewModel.VisibleParameters)
            {
                SignalXY signal;
                var dataCollection = _viewModel.DataCollections[parameter.Number];
                double min, max;
                if (dataCollection is DataCollection<float> floatDataCollection)
                {
                    signal = Chart.Plot.Add.SignalXY(_viewModel.DateTimes, floatDataCollection.Values);
                    (min, max) = MathHelper.GetMinMax<float>(floatDataCollection.Values);
                }
                else if (dataCollection is DataCollection<uint> uintDataCollection)
                {
                    signal = Chart.Plot.Add.SignalXY(_viewModel.DateTimes, uintDataCollection.Values);
                    (min, max) = MathHelper.GetMinMax<uint>(uintDataCollection.Values);
                }
                else if (dataCollection is DataCollection<ushort> ushortDataCollection)
                {
                    signal = Chart.Plot.Add.SignalXY(_viewModel.DateTimes, ushortDataCollection.Values);
                    (min, max) = MathHelper.GetMinMax<ushort>(ushortDataCollection.Values);
                }
                else continue;

                signal.Color = Color.FromSKColor(SKColor.Parse(parameter.Color));
                signal.LineWidth = 2.5f;

                var yAxis = Chart.Plot.Axes.AddLeftAxis();
                yAxis.Label.Text = $"{parameter.Name}, {parameter.Unit}";
                yAxis.Color(Color.FromSKColor(SKColor.Parse(parameter.Color)));
                yAxis.IsVisible = parameter.IsEnabled && parameter.IsAxisVisibleOnChart;
                signal.Axes.YAxis = yAxis;

                var info = new ChartInfo
                {
                    Chart = signal,
                    Parameter = parameter,
                    MinY = min,
                    MaxY = max,
                };

                info.SetYZoom(_yAxisZoom);

                _chartInfos[parameter.Number] = info;
            }

            _minX = _viewModel.DateTimes.First();
            _maxX = _viewModel.DateTimes.Last();

            Chart.Plot.Axes.SetLimitsX(_minX - 1, _maxX + 1);
            Chart.Refresh();
        }

        private void Chart_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(Chart);
            var coordinates = Chart.Plot.GetCoordinates((float)point.Position.X, (float)point.Position.Y);
            var roundedX = RoundADateToHour(coordinates.X);
            var clampedX = Math.Clamp(roundedX, _minX, _maxX);
            var roundedCoordinates = coordinates with { X = clampedX };
            if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            {
                RightButtonPressed();
            }
            if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                if (e.ClickCount == 1)
                {
                    var thingUnderMouse = GetSpanUnderMouse((float)point.Position.X, (float)point.Position.Y);
                    if (thingUnderMouse is not null && _selectionSpan is not null)
                    {
                        _spanBeingDragged = thingUnderMouse;
                        _spanLeftOffset = _spanBeingDragged.MouseStart.X - _selectionSpan.XRange.Min;
                        _spanRightOffset = _selectionSpan.XRange.Max - _spanBeingDragged.MouseStart.X;
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
                        var lineColor = Color.FromSKColor(Avalonia.Media.Colors.Red.ToSKColor()).WithAlpha(0.7);
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
                        _viewModel.IntegrationSpanActive = false;
                    }
                }
            }
        }

        private void Chart_PointerMoved(object sender, PointerEventArgs e)
        {
            var point = e.GetCurrentPoint(Chart);
            var coordinates = Chart.Plot.GetCoordinates((float)point.Position.X, (float)point.Position.Y);
            var roundedX = RoundADateToHour(coordinates.X);
            var clampedX = Math.Clamp(roundedX, _minX, _maxX);
            var roundedCoordinates = coordinates with { X = clampedX };
            if (_spanBeingDragged is not null)
            {
                if (!_spanBeingDragged.IsResizing)
                {
                    clampedX = Math.Clamp(roundedX, _minX + _spanLeftOffset, _maxX - _spanRightOffset);
                    roundedCoordinates = coordinates with { X = clampedX };
                }

                _spanBeingDragged.DragTo(roundedCoordinates);
                Cursor = new Cursor(StandardCursorType.SizeWestEast);
                Chart.Refresh();

                if (_viewModel != null && _selectionSpan != null)
                {
                    _viewModel.IntegrationSpanActive = true;
                    _viewModel.UpdateIntegration(_selectionSpan.XRange.Min, _selectionSpan.XRange.Max);
                }
            }
            else
            {
                _viewModel.CurrentDisplayDate = roundedCoordinates.X;
                _crosshair.Position = roundedCoordinates;
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
            Chart.Plot.Axes.DateTimeTicksBottom();
            Chart.Plot.Axes.Left.IsVisible = false;
            Chart.Plot.Axes.Bottom.MinimumSize = (float)(DateTime.Today.ToOADate() - DateTime.Today.Add(TimeSpan.FromHours(1)).ToOADate());
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

            _viewModel?.UpdateCurrentDisplaySpanDates(Chart.Plot.Axes.Bottom.Min, Chart.Plot.Axes.Bottom.Max);
        }

        public void ZoomIn(IPlotControl control, Pixel pixel, LockedAxes locked) => Zoom(ZoomInFactor, control, pixel);

        public void ZoomOut(IPlotControl control, Pixel pixel, LockedAxes locked) => Zoom(ZoomOutFactor, control, pixel);

        private void Zoom(double frac, IPlotControl control, Pixel pixel)
        {
            var fracX = _lockX ? 1 : frac;
            control.Plot.MouseZoom(fracX, 1, pixel);

            if (!_lockY)
            {
                _yAxisZoom *= frac;

                foreach (var info in _chartInfos.Values.Where(x => x.Parameter.IsAutoScaledOnChart))
                {
                    info.SetYZoom(_yAxisZoom);
                }
            }

            control.Refresh();

            _viewModel?.UpdateCurrentDisplaySpanDates(Chart.Plot.Axes.Bottom.Min, Chart.Plot.Axes.Bottom.Max);
        }

        private AxisSpanUnderMouse GetSpanUnderMouse(float x, float y)
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
            Chart.Plot.DataBackground = Color.FromSKColor(backgroundColor);
            Chart.Plot.FigureBackground = Color.FromSKColor(backgroundColor);

            var axesColor = App.Current!.GetBrushResource("SemiGrey9", themeVariant).Color.ToSKColor();
            void SetAxisTheme(IAxis axis)
            {
                axis.FrameLineStyle.Color = Color.FromSKColor(axesColor);
                axis.MajorTickStyle.Color = Color.FromSKColor(axesColor);
                axis.MinorTickStyle.Color = Color.FromSKColor(axesColor);
                axis.TickLabelStyle.ForeColor = Color.FromSKColor(axesColor);
            }

            SetAxisTheme(Chart.Plot.Axes.Bottom);

            var gridColor = App.Current!.GetBrushResource("SemiGrey1", themeVariant).Color.ToSKColor();
            Chart.Plot.Style.ColorGrids(Color.FromSKColor(gridColor));

            var crosshairColor = App.Current!.GetBrushResource("SemiBlue4", themeVariant).Color.ToSKColor();
            _crosshair!.LineStyle.Color = Color.FromSKColor(crosshairColor);
        }

        private sealed class ChartInfo
        {
            public SignalXY Chart { get; init; }
            public Parameter Parameter { get; set; }
            public double MinY { get; init; }
            public double MaxY { get; init; }
            public double Range => MaxY - MinY;

            public void SetYZoom(double zoom)
            {
                var margin = Range * ChartMarginMultiplier / zoom;

                Chart.Axes.YAxis.Min = MinY - margin;
                Chart.Axes.YAxis.Max = MaxY / zoom + margin;
            }
        }
    }
}