using FlomtManager.Core.Models;
using FlomtManager.Core.Providers;
using FlomtManager.Infrastructure.Data;
using FlomtManager.Infrastructure.Repositories;

var connectionString = "Data Source=app.db";

var connectionFactory = new DbConnectionFactory(connectionString);
var dbInitializer = new DbInitializer(connectionFactory);

await dbInitializer.Drop();
await dbInitializer.Init();

var deviceRepository = new DeviceRepository(connectionFactory, new TestDateTimeProvider());

var devices = await deviceRepository.GetAll();
foreach (var device in devices)
{
    Console.WriteLine($"Id: {device.Id}");
    Console.WriteLine($"Created: {device.Created}");
    Console.WriteLine($"Updated: {device.Updated}");
    Console.WriteLine($"Name: {device.Name}");
    Console.WriteLine();
}
Console.WriteLine();

foreach (var device in devices)
{
    var updateDevice = device with { Name = "Updated" };
    await deviceRepository.Update(updateDevice);
}

devices = await deviceRepository.GetAll();
foreach (var device in devices)
{
    Console.WriteLine($"Id: {device.Id}");
    Console.WriteLine($"Created: {device.Created}");
    Console.WriteLine($"Updated: {device.Updated}");
    Console.WriteLine($"Name: {device.Name}");
    Console.WriteLine();
}

var newDevice = new Device
{
    Name = "New Device"
};
var newId = await deviceRepository.Create(newDevice);
Console.WriteLine($"New device Id: {newId}");

class TestDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}