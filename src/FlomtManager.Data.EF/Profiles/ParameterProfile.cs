using AutoMapper;
using FlomtManager.Core.Models;
using FlomtManager.Data.EF.Entities;

namespace FlomtManager.Data.EF.Profiles
{
    internal class ParameterProfile : Profile
    {
        public ParameterProfile()
        {
            CreateMap<ParameterEntity, Parameter>();
            CreateMap<Parameter, ParameterEntity>();
        }
    }
}
