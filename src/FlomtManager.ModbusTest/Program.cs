﻿using FlomtManager.Modbus;
using System.Text;

var modbusProtocol = new ModbusProtocolTcp("185.147.58.54", 5000);

await modbusProtocol.OpenAsync(CancellationToken.None);

var bytes = await modbusProtocol.ReadRegistersBytesAsync(1, 0, 500, CancellationToken.None);

Console.WriteLine(string.Join(" ", bytes.Select(x => x.ToString("x2")) ?? []));
Console.WriteLine(Encoding.ASCII.GetString(bytes));

await modbusProtocol.CloseAsync(CancellationToken.None);
