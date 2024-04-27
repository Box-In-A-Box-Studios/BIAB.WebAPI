using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.CRUD;

public class Repository<TDbContext, TEntity, TId> : IDisposable
    where TDbContext : DbContext
    where TEntity : class, IHasId<TId>, new()
{
    private readonly TDbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    private readonly string? _ownerId;
    private readonly bool IsOwnedEntity = typeof(IOwnedEntity).IsAssignableFrom(typeof(TEntity));
    public Repository(TDbContext context, DbSet<TEntity> dbSet, string? ownerId = null)
    {
        _context = context;
        _dbSet = dbSet;
        _ownerId = ownerId;
        if (_ownerId == null && IsOwnedEntity) throw new UnauthorizedAccessException();
    }
    
    [Obsolete("This method is for testing purposes only.")]
    public IQueryable<TEntity> GetRaw()
    {
        return _dbSet;
    }

    public virtual IQueryable<TEntity> Get()
    {
        IQueryable<TEntity> query = _dbSet;
        
        if (typeof(INoUpdate).IsAssignableFrom(typeof(TEntity)) || typeof(IAppendUpdate<TId>).IsAssignableFrom(typeof(TEntity)))
        { // If No Update, then we don't need to track changes
            query = query.AsNoTracking();
        }
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        { // If Soft Delete, then we only want records that are not deleted
            query = query.Where(e => ((ISoftDelete)e).DeletedAt == null);
        }
        if (IsOwnedEntity)
        { // If Owned Entity, then we only want records that are owned by the user
            query = query.Where(e => ((IOwnedEntity)e).OwnerId == _ownerId);
        }
        
        return query;
    }
    
    public virtual void Create(TEntity entity)
    {
        if (IsOwnedEntity)
        { // If Owned Entity, then set the OwnerId to the user's Id
            ((IOwnedEntity)entity).OwnerId = _ownerId;
        }
        _dbSet.Add(entity);
    }

    public virtual void Update(TEntity entity)
    {
        if (IsOwnedEntity)
        {
            // Check if the entity is owned by the user
            if (((IOwnedEntity)entity).OwnerId != _ownerId)
            {
                throw new UnauthorizedAccessException();
            }
        }
        if (entity is IAppendUpdate<TId> appendUpdate) // No Update but Will Append
        {
            // Create a new record with the same data as the old record
            var newRecord = appendUpdate.Copy();
            newRecord.Id = default!;
            newRecord.OriginId = entity.Id;
            
            // Drop All Changes to the Old Record
            _context.Entry(entity).State = EntityState.Unchanged;
            Delete(entity); // Soft Delete the old record
            Create((TEntity)newRecord); // Create the new record
        }
        else if (entity is INoUpdate) // No Update
        {
            throw new Exception("This record cannot be updated.");
        }
        else // Regular Update (Overwrite)
        {
            _dbSet.Update(entity);
        }
    }
    
    
    public virtual void Delete(TEntity entity)
    {
        if (IsOwnedEntity)
        {
            // Check if the entity is owned by the user
            if (((IOwnedEntity)entity).OwnerId != _ownerId)
            {
                throw new UnauthorizedAccessException();
            }
        }
        if (entity is IHardDelete)
        { // Hard Delete
            _dbSet.Remove(entity);
        }
        else if (entity is ISoftDelete softDelete)
        { // Soft Delete
            softDelete.DeletedAt = DateTime.Now;
            _context.Entry(entity).Property("DeletedAt").IsModified = true;
        }
        else
        {
            throw new Exception("This record cannot be deleted.");
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