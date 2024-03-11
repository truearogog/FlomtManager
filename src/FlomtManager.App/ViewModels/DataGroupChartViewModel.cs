using Avalonia.Media;
using Avalonia.Skia;
using FlomtManager.App.Models;
using FlomtManager.Core.Attributes;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Services;
using FlomtManager.Framework.Extensions;
using ReactiveUI;
using ScottPlot;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace FlomtManager.App.ViewModels
{
    public class DataGroupChartViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        private double[]? _dates;
        private DataGroupValues[]? _data;

        // parameter, position
        private ReadOnlyCollection<(Parameter, int)>? _integrationParameters;

        private Device? _device;
        public Device? Device
        {
            get => _device;
            set => this.RaiseAndSetIfChanged(ref _device, value);
        }

        public ObservableCollection<ParameterViewModel> Parameters { get; set; } = [];
        public event EventHandler<IEnumerable<DataGroupValues>>? OnDataUpdate;
        public event EventHandler<byte>? OnParameterToggled;
        public event EventHandler<IntegrationChangedEventArgs>? OnIntegrationChanged;

        private double _integrationSpanMinDate;
        public double IntegrationSpanMinDate
        {
            get => _integrationSpanMinDate;
            set => this.RaiseAndSetIfChanged(ref _integrationSpanMinDate, value);
        }

        private double _integrationSpanMaxDate;
        public double IntegrationSpanMaxDate
        {
            get => _integrationSpanMaxDate;
            set => this.RaiseAndSetIfChanged(ref _integrationSpanMaxDate, value);
        }

        private double _currentDisplayDate;
        public double CurrentDisplayDate
        {
            get => _currentDisplayDate;
            set => this.RaiseAndSetIfChanged(ref _currentDisplayDate, value);
        }

        public DataGroupChartViewModel(IDataService dataService)
        {
            _dataService = dataService;

            this.WhenAnyValue(x => x.IntegrationSpanMaxDate, x => x.IntegrationSpanMinDate)
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => ChangeIntegration());

            this.WhenAnyValue(x => x.CurrentDisplayDate)
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => ChangeCurrentDataGroup());
        }

        private void ChangeCurrentDataGroup()
        {
            if (_dates != null && _data != null)
            {
                var index = _dates.BinarySearchClosestValueIndex(CurrentDisplayDate);
                var dataGroup = _data[index];
                Parameters.First().Value = CurrentDisplayDate.ToDateTime().ToString();
                foreach (var (parameter, value) in dataGroup.Parameters.Zip(dataGroup.Values))
                {
                    var parameterViewModel = Parameters.FirstOrDefault(x => x.Parameter.Number == parameter.Number);
                    if (parameterViewModel != null)
                    {
                        parameterViewModel.Value = value switch
                        {
                            float floatValue => floatValue.ToString("0.###"),
                            ushort wordValue => wordValue.ToString(),
                            DateTime dateTimeValue => dateTimeValue.ToString(),
                            _ => string.Empty
                        };
                    }
                }
            }
        }

        private void ChangeIntegration()
        {
            if (_dates != null && _data != null && _integrationParameters != null && IntegrationSpanMinDate != default && IntegrationSpanMaxDate != default)
            {
                var workingTimeIndex = _data.First().Parameters.ToList().FindIndex(x => x.ParameterType == ParameterType.WorkingTimeInSecondsInArchiveInterval);
                var minIndex = _dates.BinarySearchClosestValueIndex(IntegrationSpanMinDate);
                var maxIndex = _dates.BinarySearchClosestValueIndex(IntegrationSpanMaxDate);
                var integration = _data.Where((_, i) => i >= minIndex && i <= maxIndex)
                    .AsParallel()
                    .Aggregate(new float[_integrationParameters.Count], (integration, next) =>  
                    {
                        var workingTime = (ushort)next.Values[workingTimeIndex];
                        var current = 0;
                        foreach (var (parameter, index) in _integrationParameters)
                        {
                            integration[current++] += (float)next.Values[index] * (ushort)next.Values[workingTimeIndex] / 3600;
                        }
                        return integration;
                    });

                OnIntegrationChanged?.Invoke(this, new() {
                    IntegrationStart = DateTime.FromOADate(IntegrationSpanMinDate),
                    IntegrationEnd = DateTime.FromOADate(IntegrationSpanMaxDate),
                    IntegrationData = _integrationParameters.Select(x => x.Item1.IntegrationNumber).Zip(integration)
                });
            }
        }

        public async void UpdateData()
        {
            var dataGroups = await _dataService.GetDataGroupValues(Device?.Id ?? 0);
            var parameters = dataGroups.FirstOrDefault()?.Parameters;
            if (parameters != null)
            {
                AddParameters(parameters);
                _integrationParameters = parameters
                    .Select((x, i) => (x, i))
                    .Where(x => x.x.IntegrationNumber != 0)
                    .Select(x => (parameters.First(y => y.Number == x.x.Number), x.i))
                    .ToList().AsReadOnly();
                _data = dataGroups;
                _dates = dataGroups.Select(x => x.DateTime.ToOADate()).ToArray();
                OnDataUpdate?.Invoke(this, dataGroups);
            }
        }

        public double GetClosestDate(double date)
        {
            if (_dates != null)
            {
                var index = _dates.BinarySearchClosestValueIndex(date);
                return _dates[index];
            }
            return date;
        }

        private void AddParameters(IEnumerable<Parameter> parameters)
        {
            Parameters.Clear();
            Parameters.Add(new()
            {
                Parameter = new()
                {
                    Number = 0,
                    Comma = default,
                    ErrorMask = default,
                    IntegrationNumber = default,
                    Name = "Time",
                    Unit = string.Empty,
                    Color = Avalonia.Media.Colors.DarkGray.ToSKColor().ToString(),
                    ParameterType = ParameterType.Time
                }
            });
            foreach (var parameter in parameters)
            {
                if (parameter.ParameterType.GetAttribute<HideAttribute>()?.Hide(HideTargets.Chart) != true)
                {
                    Parameters.Add(new() { Parameter = parameter });
                }
            }
        }

        public void ToggleParameter(byte parameterNumber)
        {
            OnParameterToggled?.Invoke(this, parameterNumber);
        }
    }
}
