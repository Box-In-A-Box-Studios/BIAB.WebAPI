using BIAB.WebAPI.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.CRUD;

public class AccessorRepository<TDbContext, TAccessorEntity, TRelatedEntity, TId> : IDisposable
    where TDbContext : DbContext
    where TRelatedEntity : class, IHasAccessor<TId, TRelatedEntity, TAccessorEntity>, IHasId<TId>, IOwnedEntity, new()
    where TAccessorEntity : class, IAccessorEntity<TId, TRelatedEntity>, new()
    where TId : struct
{
    private readonly TDbContext _context;
    private readonly DbSet<TAccessorEntity> _dbSet;
    private readonly string? _ownerId;
    public AccessorRepository(TDbContext context, DbSet<TAccessorEntity> dbSet, string ownerId)
    {
        _context = context;
        _dbSet = dbSet;
        _ownerId = ownerId;
    }
    
    [Obsolete("This method is for testing purposes only.")]
    public IQueryable<TAccessorEntity> GetRaw()
    {
        return _dbSet;
    }
    
    public virtual IQueryable<TAccessorEntity> Get()
    {
        IQueryable<TAccessorEntity> query = _dbSet;
        // We know its only IHardDelete because we are using the AccessorRepository
        // We only want to return records that have the current user as the owner
        query = query.Where(e => (e).OwnerId == _ownerId);

        query = query.Include(x => x.Relation);
        
        return query.AsNoTracking();
    }
    
    
    public virtual void AddToEntity(TRelatedEntity entity, string additionalOwnerId, AccessorType accessorType)
    {
        TAccessorEntity accessor = new TAccessorEntity
        {
            OwnerId = additionalOwnerId,
            RelationId = entity.Id,
            AccessorType = accessorType
        };

        entity.Accessors.Add(accessor);
    }
    
    public virtual void RemoveFromEntity(TRelatedEntity entity, string additionalOwnerId, AccessorType accessorType)
    {
        TAccessorEntity? accessor = entity.Accessors.FirstOrDefault(e => e.OwnerId == additionalOwnerId && e.AccessorType == accessorType);
        if (accessor != null)
        {
            entity.Accessors.Remove(accessor);
        }
    }
    
    public virtual void SaveChanges()
    {
        _context.SaveChanges();
    }
    
    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
    
    public virtual void Dispose()
    {
        _context.Dispose();
    }
}