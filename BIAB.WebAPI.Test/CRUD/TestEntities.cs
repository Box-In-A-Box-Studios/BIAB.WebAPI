using BIAB.WebAPI.CRUD;
using BIAB.WebAPI.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace BIAB.WebAPI.Test.CRUD;

public class TestEntity : IHasId<int>
{
    public int Id { get; set; }
    public string? Text { get; set; }
}

public class TestEntityWithHardDelete : TestEntity, IHardDelete
{
    
}

public class TestEntityNoUpdate : TestEntity, INoUpdate
{
    
}


public class TestEntityWithSoftDelete : TestEntity, ISoftDelete
{
    public DateTime? DeletedAt { get; set; }
}

public class TestEntityAppendUpdate : TestEntity, IAppendUpdate<int>
{
    public DateTime? DeletedAt { get; set; }
    public int OriginId { get; set; }
    public IAppendUpdate<int> Copy()
    {
        return new TestEntityAppendUpdate
        {
            Id = Id,
            Text = Text,
            OriginId = Id
        };
    }
}

public class OwnedHardDeleteEntity : TestEntityWithHardDelete, IOwnedEntity
{
    public string? OwnerId { get; set; }
    public IdentityUser? Owner { get; set; }
}

public class OwnedSoftDeleteEntity : TestEntityWithSoftDelete, IOwnedEntity
{
    public string? OwnerId { get; set; }
    public IdentityUser? Owner { get; set; }
}

public class OwnedAppendUpdateEntity : TestEntityAppendUpdate, IOwnedEntity, IAppendUpdate<int>
{
    public string? OwnerId { get; set; }
    
    public new IAppendUpdate<int> Copy()
    {
        return new OwnedAppendUpdateEntity
        {
            Id = Id,
            Text = Text,
            OriginId = OriginId,
            OwnerId = OwnerId
        };
    }
}

public class TestEntityAccessor : TestEntity, IAccessorEntity<int, TestEntityWithAccessor>
{
    public string? OwnerId { get; set; }
    public IdentityUser? Owner { get; set; }
    public int RelationId { get; set; }
    public TestEntityWithAccessor? Relation { get; set; }
    public AccessorType AccessorType { get; set; }
}

public class TestEntityWithAccessor : TestEntity, IOwnedEntity, IHasAccessor<int, TestEntityWithAccessor, TestEntityAccessor>
{
    public string? OwnerId { get; set; }
    public IdentityUser? Owner { get; set; }
    
    public List<TestEntityAccessor> Accessors { get; set; } = new();
}