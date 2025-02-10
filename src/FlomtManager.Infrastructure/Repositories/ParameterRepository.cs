using FlomtManager.Core;
using FlomtManager.Core.Entities;
using FlomtManager.Core.Repositories;

namespace FlomtManager.Infrastructure.Repositories;

internal sealed class ParameterRepository(IAppDb appDb) : Repository<Parameter>(appDb, appDb.Parameters), IParameterRepository;
