using System.IO.Ports;
using FlomtManager.Core.Enums;

namespace FlomtManager.Core.Models;

public readonly record struct Device(
    int Id, 
    DateTime Created, 
    DateTime Updated, 

    string Name, 
    string Address, 

    ConnectionType ConnectionType, 
    byte SlaveId, 

    byte ReadSeconds,
    byte ReadMinutes,
    byte ReadHours,

    bool ReadArchivesAutomatic,

    string IpAddress,
    string PortName, 

    int BaudRate, 
    Parity Parity, 
    int DataBits, 
    StopBits StopBits, 
    int Port);