using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Skia;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Events;
using FlomtManager.Core.Models;
using FlomtManager.Core.Models.Collections;
using FlomtManager.Core.Parsers;
using FlomtManager.Core.Repositories;
using FlomtManager.Framework.Extensions;
using FlomtManager.Framework.Helpers;
using ReactiveUI;

namespace FlomtManager.App.ViewModels
{
    public class DataGroupChartViewModel : ViewModel
    {
        private readonly IDataFormatter _dataFormatter;
        private readonly IDataRepository _dataRepository;
        private readonly IParameterRepository _parameterRepository;

        public IReadOnlyDictionary<Parameter, Parameter> IntegralParameters { get; private set; }
        public IEnumerable<Parameter> VisibleParameters { get; private set; }
        public IReadOnlyDictionary<byte, IDataCollection> DataCollections { get; private set; }
        public double[] DateTimes { get; private set; }
        public float[] WorkingTimeCollectionFloat { get; private set; }
        public uint[] WorkingTimeCollectionUint { get; private set; }
        public ushort[] WorkingTimeCollectionUshort { get; private set; }

        private Device _device;
        public Device Device
        {
            get => _device;
            set => this.RaiseAndSetIfChanged(ref _device, value);
        }

        public ObservableCollection<ParameterViewModel> Parameters { get; set; } = [];

        public event EventHandler OnDataUpdated;
        public event EventHandler<byte> OnParameterToggled;
        public event EventHandler<IntegrationChangedArgs> OnIntegrationChanged;

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

        public DataGroupChartViewModel(
            IDataFormatter dataFormatter,
            IDataRepository dataRepository,
            IParameterRepository parameterRepository)
        {
            _dataFormatter = dataFormatter;
            _dataRepository = dataRepository;
            _parameterRepository = parameterRepository;

            this.WhenAnyValue(x => x.IntegrationSpanMaxDate, x => x.IntegrationSpanMinDate)
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => ChangeIntegration());

            this.WhenAnyValue(x => x.CurrentDisplayDate)
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => ChangeSelectedDataPoint());
        }

        public async Task SetDevice(Device device)
        {
            Device = device;
            await UpdateParameters();
        }

        private async Task UpdateParameters()
        {
            if (Device == default)
            {
                return;
            }

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
                    Type = ParameterType.Time,
                    ChartYScalingType = ChartScalingType.Auto,
                    ChartYZoom = 1
                }
            });

            var parameters = await _parameterRepository.GetHourArchiveParametersByDeviceId(Device.Id);
            foreach (var parameter in parameters)
            {
                Parameters.Add(new() { Parameter = parameter });
            }

            IntegralParameters = await _parameterRepository.GetHourArchiveIntegralParametersByDeviceId(Device.Id);
            VisibleParameters = parameters;
        }

        public async Task UpdateData()
        {
            if (!await _dataRepository.HasHourData(Device.Id))
            {
                return;
            }

            var dataCollections = await _dataRepository.GetHourData(Device.Id);
            var parameters = await _parameterRepository.GetHourArchiveParametersByDeviceId(Device.Id, true);

            var workingTimeParameter = parameters.FirstOrDefault(x => x.Type == ParameterType.WorkingTimeInSecondsInArchiveInterval);
            if (workingTimeParameter == default)
            {
                return;
            }

            DateTimes = ((DataCollection<DateTime>)dataCollections[0]).Values.Select(x => x.ToOADate()).ToArray();

            var workingTimeCollection = (DataCollection<ushort>)dataCollections[workingTimeParameter.Number];
            WorkingTimeCollectionFloat = workingTimeCollection.Values.Select(x => (float)x).ToArray();
            WorkingTimeCollectionUint = workingTimeCollection.Values.Select(x => (uint)x).ToArray();
            WorkingTimeCollectionUshort = workingTimeCollection.Values;
            DataCollections = dataCollections;

            OnDataUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void ChangeSelectedDataPoint()
        {
            if (DateTimes is null || DataCollections is null)
            {
                return;
            }

            var index = DateTimes.BinarySearchClosestValueIndex(CurrentDisplayDate);
            if (index != -1)
            {
                foreach (var parameterViewModel in Parameters)
                {
                    var parameter = parameterViewModel.Parameter;
                    if (DataCollections[parameter.Number] is DataCollection<float> floatDataCollection)
                    {
                        parameterViewModel.Value = _dataFormatter.FormatFloat(floatDataCollection.Values[index], parameter);
                    }
                    else if (DataCollections[parameter.Number] is DataCollection<uint> uintDataCollection)
                    {
                        parameterViewModel.Value = _dataFormatter.FormatUInt32(uintDataCollection.Values[index]);
                    }
                    else if (DataCollections[parameter.Number] is DataCollection<ushort> shortDataCollection)
                    {
                        parameterViewModel.Value = _dataFormatter.FormatUInt16(shortDataCollection.Values[index]);
                    }
                    else if (DataCollections[parameter.Number] is DataCollection<TimeSpan> timeSpanDataCollection)
                    {
                        parameterViewModel.Value = _dataFormatter.FormatTimeSpan(timeSpanDataCollection.Values[index]);
                    }
                    else if (DataCollections[parameter.Number] is DataCollection<DateTime> dateTimeDataCollection)
                    {
                        parameterViewModel.Value = _dataFormatter.FormatDateTime(dateTimeDataCollection.Values[index]);
                    }
                }
            }
        }

        private void ChangeIntegration()
        {
            if (DateTimes is null || DataCollections is null || 
                IntegrationSpanMinDate == default || IntegrationSpanMaxDate == default)
            {
                return;
            }

            var minIndex = DateTimes.BinarySearchClosestValueIndex(IntegrationSpanMinDate);
            var maxIndex = DateTimes.BinarySearchClosestValueIndex(IntegrationSpanMaxDate) + 1;

            Dictionary<byte, string> integrationResults = [];

            foreach (var (parameter, integrationParameter) in IntegralParameters)
            {
                if (minIndex > maxIndex)
                {
                    integrationResults[integrationParameter.Number] = string.Empty;
                    continue;
                }

                if (DataCollections[parameter.Number] is DataCollection<float> floatDataCollection)
                {
                    var sum = MathHelper.Integrate(floatDataCollection.Values.AsSpan(minIndex, maxIndex - minIndex), WorkingTimeCollectionFloat.AsSpan(minIndex, maxIndex - minIndex));
                    integrationResults[integrationParameter.Number] = _dataFormatter.FormatFloat(sum, integrationParameter);
                }
                else if (DataCollections[parameter.Number] is DataCollection<uint> uintDataCollection)
                {
                    var sum = MathHelper.Integrate(uintDataCollection.Values.AsSpan(minIndex, maxIndex - minIndex), WorkingTimeCollectionUint.AsSpan(minIndex, maxIndex - minIndex));
                    integrationResults[integrationParameter.Number] = _dataFormatter.FormatUInt32(sum);
                }
                else if (DataCollections[parameter.Number] is DataCollection<ushort> ushortDataCollection)
                {
                    var sum = MathHelper.Integrate(ushortDataCollection.Values.AsSpan(minIndex, maxIndex - minIndex), WorkingTimeCollectionUshort.AsSpan(minIndex, maxIndex - minIndex));
                    integrationResults[integrationParameter.Number] = _dataFormatter.FormatUInt16(sum);
                }
            }

            OnIntegrationChanged?.Invoke(this, new IntegrationChangedArgs
            {
                IntegrationStart = DateTime.FromOADate(IntegrationSpanMinDate),
                IntegrationEnd = DateTime.FromOADate(IntegrationSpanMaxDate),
                IntegratedValues = integrationResults,
            });
        }

        public void ToggleParameter(byte parameterNumber)
        {
            OnParameterToggled?.Invoke(this, parameterNumber);
        }
    }
}
