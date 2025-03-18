using System.IO.Ports;
using FlomtManager.Domain.Enums;

namespace FlomtManager.Domain.Models;

public readonly record struct Device(
    int Id,
    DateTime Created,
    DateTime Updated,

    string Name,
    string Address,

    ConnectionType ConnectionType,
    byte SlaveId,
    byte DataReadIntervalHours,
    byte DataReadIntervalMinutes,
    byte DataReadIntervalSeconds,

    string PortName,
    int BaudRate,
    Parity Parity,
    int DataBits,
    StopBits StopBits,

    string IpAddress,
    int Port);