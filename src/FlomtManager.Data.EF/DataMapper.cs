using AutoMapper;

namespace FlomtManager.Data.EF
{
    internal class DataMapper(IConfigurationProvider configuration) : Mapper(configuration), IDataMapper
    {
    }
}
