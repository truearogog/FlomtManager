using FlomtManager.Core;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Repositories;

namespace FlomtManager.Infrastructure.Repositories;

internal sealed class DeviceRepository(IAppDb appDb) : Repository<Device>(appDb, appDb.Devices), IDeviceRepository;
