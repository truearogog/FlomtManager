using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Data.EF.Entities;
using FlomtManager.Data.EF.Repositories.Base;

namespace FlomtManager.Data.EF.Repositories
{
    internal class DataGroupRepository(IAppDb db, IDataMapper mapper)
        : RepositoryBase<DataGroupEntity, DataGroup>(db, db.DataGroups, mapper), IDataGroupRepository
    {
    }
}
