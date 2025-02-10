using FlomtManager.Core;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Repositories;

namespace FlomtManager.Infrastructure.Repositories;

internal sealed class DeviceDefinitionRepository(IAppDb appDb) : Repository<DeviceDefinition>(appDb, appDb.DeviceDefinitions), IDeviceDefinitionRepository;
