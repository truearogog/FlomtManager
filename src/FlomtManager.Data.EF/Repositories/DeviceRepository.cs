using AutoMapper.QueryableExtensions;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Data.EF.Entities;
using FlomtManager.Data.EF.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlomtManager.Data.EF.Repositories
{
    internal class DeviceRepository(IAppDb db, IDataMapper mapper) 
        : RepositoryBase<DeviceEntity, Device>(db, db.Devices, mapper), IDeviceRepository
    {
        public override IQueryable<Device> GetAllQueryable()
        {
            return DbSet.AsNoTracking()
                .OrderByDescending(x => x.Updated)
                .Include(x => x.Parameters)
                .Include(x => x.DeviceDefinition)
                .ProjectTo<Device>(MapperConfig);
        }

        public override IQueryable<Device> GetAllQueryable(Expression<Func<Device, bool>> predicate)
        {
            return DbSet.AsNoTracking()
                .OrderByDescending(x => x.Updated)
                .Include(x => x.Parameters)
                .Include(x => x.DeviceDefinition)
                .ProjectTo<Device>(MapperConfig)
                .Where(predicate);
        }
    }
}
