using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Data.EF.Entities;
using FlomtManager.Data.EF.Repositories.Base;

namespace FlomtManager.Data.EF.Repositories
{
    internal class ParameterRepository(IAppDb db, IDataMapper mapper)
        : RepositoryBase<ParameterEntity, Parameter>(db, db.Parameters, mapper), IParameterRepository
    {
    }
}
