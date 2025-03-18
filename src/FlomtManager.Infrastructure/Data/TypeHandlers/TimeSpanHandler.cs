using System.Data;
using static Dapper.SqlMapper;

namespace FlomtManager.Infrastructure.Data.TypeHandlers;

internal sealed class TimeSpanHandler : TypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value)
    {
        return TimeSpan.Parse((string)value);
    }

    public override void SetValue(IDbDataParameter parameter, TimeSpan value)
    {
        throw new InvalidOperationException("Never called!");
        //parameter.Value = value.ToString();
    }
}
