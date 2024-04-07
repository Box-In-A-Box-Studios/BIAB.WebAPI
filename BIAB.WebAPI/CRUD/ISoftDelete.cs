namespace BIAB.WebAPI.CRUD;

// Flag Interface for Soft Deletes (Marking Records as Deleted)
public interface ISoftDelete
{
    public DateTime? DeletedAt { get; set; }
}