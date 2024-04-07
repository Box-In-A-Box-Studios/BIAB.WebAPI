namespace BIAB.WebAPI.CRUD;

// Flag Interface for Records that have an Id
public interface IHasId<T>
{
    public T Id { get; set; }
}