using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.Repositories
{
    public interface IParameterRepository
    {
        Task<int> Create(Parameter parameter);
        Task Create(IEnumerable<Parameter> parameters);
        Task UpdateIsEnabled(int id, bool isEnabled);
        Task UpdateIsAxisVisibleOnChart(int id, bool isAxisVisibleOnChart);
        Task UpdateIsAutoScaledOnChart(int id, bool isAutoScaledOnChart);
        Task UpdateZoomLevelOnChart(int id, double zoomLevelOnChart);
        Task UpdateColor(int id, string color);
        Task<IEnumerable<Parameter>> GetAllByDeviceId(int deviceId);
        Task<IEnumerable<Parameter>> GetCurrentParametersByDeviceId(int deviceId, bool all = false);
        Task<IEnumerable<Parameter>> GetIntegralParametersByDeviceId(int deviceId, bool all = false);
        Task<IEnumerable<Parameter>> GetHourArchiveParametersByDeviceId(int deviceId, bool all = false);
        Task<IReadOnlyDictionary<Parameter, Parameter>> GetHourArchiveIntegralParametersByDeviceId(int deviceId);
    }
}
