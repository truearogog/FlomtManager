using AutoMapper;
using FlomtManager.Core.Models;
using FlomtManager.App.ViewModels;

namespace FlomtManager.App.Profiles
{
    internal class DeviceProfile : Profile
    {
        public DeviceProfile()
        {
            CreateMap<Device, DeviceFormViewModel>();
            CreateMap<DeviceFormViewModel, Device>();
        }
    }
}
