namespace BIAB.WebAPI.CRUD;

public interface IHasAccessor<TKey, TEntity, TAccessorEntity> 
    where TEntity : IHasId<TKey>, IOwnedEntity
    where TAccessorEntity : IAccessorEntity<TKey,TEntity>
{
    public List<TAccessorEntity> Accessors { get; set; }
}