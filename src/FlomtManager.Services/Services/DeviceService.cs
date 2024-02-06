using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;
using Serilog;

namespace FlomtManager.Services.Services
{
    public class DeviceService(IDeviceRepository deviceRepository) : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository = deviceRepository;

        public IAsyncEnumerable<Device> GetAll()
            => _deviceRepository.GetAll();

        public async Task<Device> GetById(int id)
            => await _deviceRepository.GetById(id);

        public async Task<bool> CreateDevice(Device device)
            => await Execute(_deviceRepository.Create(device));

        public async Task<bool> UpdateDevice(Device device)
            => await Execute(_deviceRepository.Update(device));

        public async Task<bool> DeleteDevice(int id)
            => await Execute(_deviceRepository.Delete(id));

        private static async Task<bool> Execute(Task action)
        {
            try
            {
                await action;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Empty);
                return false;
            }
        }
    }
}
