using AutoMapper;
using FlomtManager.Core.Models;
using FlomtManager.UI.ViewModels;

namespace FlomtManager.UI.Profiles
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
