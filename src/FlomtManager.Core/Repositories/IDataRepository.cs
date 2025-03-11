using FlomtManager.Core.Models.Collections;

namespace FlomtManager.Core.Repositories;

public interface IDataRepository
{
    Task InitDataTables(int deviceId);

    Task<bool> HasHourData(int deviceId);

    Task AddHourData(int deviceId, IReadOnlyDictionary<byte, IDataCollection> data, int size);

    Task<IReadOnlyDictionary<byte, IDataCollection>> GetHourData(int deviceId);
}
