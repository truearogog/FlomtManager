using AutoMapper;
using FlomtManager.Core.Models;
using FlomtManager.Data.EF.Entities;

namespace FlomtManager.Data.EF.Profiles
{
    internal class DeviceProfile : Profile
    {
        public DeviceProfile()
        {
            CreateMap<DeviceEntity, Device>();
            CreateMap<Device, DeviceEntity>();
        }
    }
}
