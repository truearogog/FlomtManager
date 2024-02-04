using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Data.EF.Entities;
using FlomtManager.Data.EF.Repositories.Base;

namespace FlomtManager.Data.EF.Repositories
{
    internal class DeviceRepository(IAppDb db, IDataMapper mapper) 
        : RepositoryBase<DeviceEntity, Device>(db, db.Devices, mapper), IDeviceRepository
    {
    }
}
