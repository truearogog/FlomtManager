using AutoMapper;
using FlomtManager.Core.Models;
using FlomtManager.Data.EF.Entities;

namespace FlomtManager.Data.EF.Profiles
{
    internal class DataGroupProfile : Profile
    {
        public DataGroupProfile()
        {
            CreateMap<DataGroupEntity, DataGroup>();
            CreateMap<DataGroup, DataGroupEntity>();
        }
    }
}
