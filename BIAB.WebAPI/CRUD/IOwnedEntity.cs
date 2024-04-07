namespace BIAB.WebAPI.CRUD;

// Implement this Interface on Entities that are Owned by another Entity
public interface IOwnedEntity
{
    public string? OwnerId { get; set; }
}
