using AutoMapper;

namespace FlomtManager.Data.EF
{
    public class DataMapper(IConfigurationProvider configuration) : Mapper(configuration), IDataMapper
    {
    }
}
