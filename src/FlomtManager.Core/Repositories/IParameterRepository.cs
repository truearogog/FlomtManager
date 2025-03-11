using FlomtManager.Core.Models;

namespace FlomtManager.Core.Repositories
{
    public interface IParameterRepository
    {
        Task<int> Create(Parameter parameter);
        Task Create(IEnumerable<Parameter> parameters);
        Task<IEnumerable<Parameter>> GetAllByDeviceId(int deviceId);
        Task<IEnumerable<Parameter>> GetCurrentParametersByDeviceId(int deviceId, bool all = false);
        Task<IEnumerable<Parameter>> GetIntegralParametersByDeviceId(int deviceId, bool all = false);
        Task<IEnumerable<Parameter>> GetHourArchiveParametersByDeviceId(int deviceId, bool all = false);
        Task<IReadOnlyDictionary<Parameter, Parameter>> GetHourArchiveIntegralParametersByDeviceId(int deviceId);
    }
}
