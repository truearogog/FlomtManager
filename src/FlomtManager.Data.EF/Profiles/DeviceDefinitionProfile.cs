using AutoMapper;
using FlomtManager.Core.Models;
using FlomtManager.Data.EF.Entities;

namespace FlomtManager.Data.EF.Profiles
{
    internal class DeviceDefinitionProfile : Profile
    {
        public DeviceDefinitionProfile()
        {
            CreateMap<DeviceDefinitionEntity, DeviceDefinition>();
            CreateMap<DeviceDefinition, DeviceDefinitionEntity>();
        }
    }
}
