using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;

namespace FlomtManager.Services.Services
{
    public class DeviceService(IDeviceRepository deviceRepository) : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository = deviceRepository;

        public async Task<IEnumerable<Device>> GetAll()
        {
            return await _deviceRepository.GetAll();
        }

        public async Task<Device> GetById(int id)
        {
            return await _deviceRepository.GetById(id);
        }

        public async Task<bool> CreateDevice(Device device)
        {
            try
            {
                await _deviceRepository.Create(device);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateDevice(Device device)
        {
            try
            {
                await _deviceRepository.Update(device);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteDevice(int id)
        {
            try
            {
                await _deviceRepository.Delete(id);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
