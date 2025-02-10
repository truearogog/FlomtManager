using FlomtManager.Core;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Repositories;

namespace FlomtManager.Infrastructure.Repositories;

internal sealed class DataGroupRepository(IAppDb appDb) : Repository<DataGroup>(appDb, appDb.DataGroups), IDataGroupRepository;
