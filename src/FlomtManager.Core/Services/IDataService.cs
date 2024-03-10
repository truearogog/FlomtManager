using FlomtManager.Core.Models;

namespace FlomtManager.Core.Services
{
    public interface IDataService
    {
        Task<DataGroupValues[]> GetDataGroupValues(int deviceId, CancellationToken cancellationToken = default);
    }
}
