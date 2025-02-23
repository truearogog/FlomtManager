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
using Parameter = FlomtManager.Core.Entities.Parameter;

namespace FlomtManager.App.Views
{
    public partial class DataGroupChart : UserControl
    {
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

        private DataGroupChartViewModel _viewModel;

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

            _minX = dataGroups.First().DateTime.ToOADate();
            _maxX = dataGroups.Last().DateTime.ToOADate();

            foreach (var chartInfo in _chartInfos.Values)
            {
                Chart.Plot.Remove(chartInfo.Chart);
                _chartInfos.Remove(chartInfo.Parameter.Number);
            }

            Chart.Plot.Axes.Remove(Edge.Left);

            var current = 0;
            var xs = dataGroups.Select(x => x.DateTime.ToOADate()).ToArray();
            foreach (var parameter in parameters)
            {
                if (parameter.ParameterType.GetAttribute<HideAttribute>()?.Hide(HideTargets.Chart) != true)
                {
                    var ys = dataGroups.Select(x => x.Values[current]);
                    if (ys.First().GetType() == typeof(float))
                    {
                        var floatYs = ys.OfType<float>().ToArray();

                        var signalXY = Chart.Plot.Add.SignalXY(xs, floatYs);
                        signalXY.Color = Color.FromSKColor(SKColor.Parse(parameter.Color));
                        signalXY.LineStyle.Width = 2.5f;

                        var yAxis = Chart.Plot.Axes.AddLeftAxis();
                        yAxis.IsVisible = (DataContext as DataGroupChartViewModel)?.YAxesVisible ?? false;
                        yAxis.Label.Text = $"{parameter.Name}, {parameter.Unit}";
                        yAxis.Color(Color.FromSKColor(SKColor.Parse(parameter.Color)));
                        signalXY.Axes.YAxis = yAxis;

                        var info = new ChartInfo
                        {
                            Chart = signalXY,
                            Parameter = parameter,
                            MinY = floatYs.Min(),
                            MaxY = floatYs.Max(),
                        };

                        _chartInfos[parameter.Number] = info;
                    }
                }
                current++;
            }

            Chart.Plot.Axes.SetLimitsX(xs.First() - 1, xs.Last() + 1);
            Chart.Plot.Axes.AutoScaleY();
            Chart.Refresh();
        }

        private void _OnParameterToggled(object sender, byte parameterNumber)
        {
            if (_chartInfos.TryGetValue(parameterNumber, out var chartInfo))
            {
                var chart = chartInfo.Chart;
                chart.IsVisible = !chart.IsVisible;
                chart.Axes.YAxis!.IsVisible = chart.IsVisible && ((DataContext as DataGroupChartViewModel)?.YAxesVisible ?? false);
                Chart.Refresh();
            }
        }

        private void _OnYAxesToggled(object sender, bool visible)
        {
            foreach (var chartInfo in _chartInfos.Values.Where(x => x.Chart.IsVisible))
            {
                chartInfo.Chart.Axes.YAxis!.IsVisible = visible;
            }
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

        private const double ZoomInFactor = 1.2;
        public void ZoomIn(IPlotControl control, Pixel pixel, LockedAxes locked) => Zoom(ZoomInFactor, control, pixel);

        private const double ZoomOutFactor = 1 / ZoomInFactor;
        public void ZoomOut(IPlotControl control, Pixel pixel, LockedAxes locked) => Zoom(ZoomOutFactor, control, pixel);

        private void Zoom(double frac, IPlotControl control, Pixel pixel)
        {
            var fracX = _lockX ? 1 : frac;
            control.Plot.MouseZoom(fracX, 1, pixel);

            if (!_lockY)
            {
                _yAxisZoom *= frac;

                foreach (var chartInfo in _chartInfos.Values.Where(x => x.Parameter.ChartYScalingType == ChartScalingType.Auto))
                {
                    var chart = chartInfo.Chart;
                    chart.Axes.YAxis.Range.ZoomFrac(frac, (chartInfo.MinY + chartInfo.MaxY) / 2);
                }
            }

            control.Refresh();
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

            var chartAxes = _chartInfos.Values.Select(x => x.Chart.Axes.YAxis.GetHashCode()).ToHashSet();
            if (!chartAxes.Contains(Chart.Plot.Axes.Left.GetHashCode()))
            {
                SetAxisTheme(Chart.Plot.Axes.Left);
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
            public Parameter Parameter { get; init; }
            public double MinY { get; init; }
            public double MaxY { get; init; }
        }
    }
}