namespace BIAB.WebAPI.CRUD;

// Flag Interface for Records that Append Updates instead of Overwriting
// T is Type of Object that is the Id of the Record
public interface IAppendUpdate<T> : IHasId<T>, ISoftDelete, INoUpdate
{ 
    public T? OriginId { get; set; }

    public IAppendUpdate<T> Copy(); // Force Copy The Variables to a new Reference
}