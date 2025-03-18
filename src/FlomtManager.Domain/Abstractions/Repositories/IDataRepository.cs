using FlomtManager.Domain.Models.Collections;

namespace FlomtManager.Domain.Abstractions.Repositories;

public interface IDataRepository
{
    Task InitDataTables(int deviceId);

    Task<bool> HasHourData(int deviceId);

    Task AddHourData(int deviceId, IReadOnlyDictionary<byte, IDataCollection> data, int size, int actualSize);

    Task<IReadOnlyDictionary<byte, IDataCollection>> GetHourData(int deviceId);
}
