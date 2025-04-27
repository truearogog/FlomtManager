using FlomtManager.Domain.Abstractions.Stores;
using FlomtManager.Domain.Models;

namespace FlomtManager.Infrastructure.Stores;

internal sealed class ParameterStore : Store<Parameter>, IParameterStore
{

}
