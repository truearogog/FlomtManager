using System.ComponentModel.DataAnnotations;
using System.IO.Ports;
using FlomtManager.Domain.Abstractions.ViewModels;
using FlomtManager.Domain.Enums;
using FlomtManager.Domain.Models;
using ReactiveUI;

namespace FlomtManager.Application.ViewModels;

internal sealed class DeviceFormViewModel : ViewModel, IDeviceFormViewModel
{
    private int _id = 0;
    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    private string _name;
    [Required]
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _address;
    public string Address
    {
        get => _address;
        set => this.RaiseAndSetIfChanged(ref _address, value);
    }

    private ConnectionType _connectionType = ConnectionType.Serial;
    public ConnectionType ConnectionType
    {
        get => _connectionType;
        set => this.RaiseAndSetIfChanged(ref _connectionType, value);
    }

    private byte _slaveId = 0;
    public byte SlaveId
    {
        get => _slaveId;
        set => this.RaiseAndSetIfChanged(ref _slaveId, value);
    }

    private TimeSpan _dataReadInterval = TimeSpan.FromSeconds(5);
    public TimeSpan DataReadInterval
    {
        get => _dataReadInterval;
        set => this.RaiseAndSetIfChanged(ref _dataReadInterval, value);
    }

    private string _portName = "COM1";
    public string PortName
    {
        get => _portName;
        set => this.RaiseAndSetIfChanged(ref _portName, value);
    }

    private int _baudRate = 57600;
    public int BaudRate
    {
        get => _baudRate;
        set => this.RaiseAndSetIfChanged(ref _baudRate, value);
    }

    private Parity _parity = Parity.None;
    public Parity Parity
    {
        get => _parity;
        set => this.RaiseAndSetIfChanged(ref _parity, value);
    }

    private int _dataBits = 8;
    public int DataBits
    {
        get => _dataBits;
        set => this.RaiseAndSetIfChanged(ref _dataBits, value);
    }

    private StopBits _stopBits = StopBits.One;
    public StopBits StopBits
    {
        get => _stopBits;
        set => this.RaiseAndSetIfChanged(ref _stopBits, value);
    }

    private string _ipAddress;
    public string IpAddress
    {
        get => _ipAddress;
        set => this.RaiseAndSetIfChanged(ref _ipAddress, value);
    }

    private int _port;
    public int Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }

    public Device GetDevice()
    {
        return new Device
        {
            Id = Id,

            Name = Name,
            Address = Address,

            ConnectionType = ConnectionType,
            SlaveId = SlaveId,
            DataReadIntervalTicks = DataReadInterval == TimeSpan.Zero 
                ? TimeSpan.FromSeconds(5).Ticks 
                : DataReadInterval.Ticks,

            PortName = PortName,
            BaudRate = BaudRate,
            Parity = Parity,
            DataBits = DataBits,
            StopBits = StopBits,

            IpAddress = IpAddress,
            Port = Port,
        };
    }
}
