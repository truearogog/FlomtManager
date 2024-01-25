using AutoMapper;

namespace FlomtManager.Data.EF.Repositories.Base
{
    public abstract class RepositoryBase(IAppDb db, IDataMapper mapper)
    {
        protected readonly IAppDb Db = db;
        protected IDataMapper Mapper = mapper;
        protected IConfigurationProvider MapperConfig => Mapper.ConfigurationProvider;
    }
}
