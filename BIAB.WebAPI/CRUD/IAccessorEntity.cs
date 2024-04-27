using BIAB.WebAPI.Shared.Enums;

namespace BIAB.WebAPI.CRUD;

public interface IAccessorEntity<TKey, TEntity> : IOwnedEntity, IHardDelete where TEntity : IHasId<TKey>, IOwnedEntity
{
    public TKey? RelationId { get; set; }
    public TEntity? Relation { get; set; }
    
    public AccessorType AccessorType { get; set; }
}