using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Data.EF.Entities;
using FlomtManager.Data.EF.Repositories.Base;

namespace FlomtManager.Data.EF.Repositories
{
    internal class DeviceDefinitionRepository(IAppDb db, IDataMapper mapper)
        : RepositoryBase<DeviceDefinitionEntity, DeviceDefinition>(db, db.DeviceDefinitions, mapper), IDeviceDefinitionRepository
    {
    }
}
