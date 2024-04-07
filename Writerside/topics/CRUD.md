# CRUD

**Last Updated**: April 7th, 2024


## Introduction

**Namespace**: BIAB.WebAPI.CRUD

CRUD stands for Create, Read, Update, and Delete. It is a set of operations that can be performed on a database or data structure. It is a common way to interact with databases and is used in many applications.

BIAB.WebAPI.CRUD is a library that provides a simple way to perform CRUD operations on a database. It is designed to be easy to use and flexible, allowing you to work with different types of databases and data structures.


## Repository
This is a base class that will automatically handle the entity's CRUD operations. It is designed to be easy to use and flexible, allowing you to work with different types of databases and data structures.

**Example**:
```csharp

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}

public class TestEntity : IHasId<int>, IHardDelete
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class TestRepository : Repository<TestDbContext, TestEntity, int>
{
    public TestRepository(DbContext context) : base(context) { }
}
```

## Entity Flags
- **[IHasId](#ihasid)**: An entity that has an Id property.
- **[ISoftDelete](#isoftdelete)**: An entity that will soft delete. (Delete without removing from the database)
- **[IHardDelete](#iharddelete)**: An entity that will hard delete. (Delete and remove from the database)
- **[INoUpdate](#inoupdate)**: An entity that will not update. (Entity will Error if An Update is attempted)
- **[IAppendUpdate](#iappendupdate)**: An entity that will soft delete current entity and append a new entity with the changes.
- **[IOwnedEntity](#iownedentity)**: An entity that is owned by an Identity User. (only that user can access the entity)


### IHasId
An entity that has an Id property.

Note: Id is a generic type, so you can use any struct type as the Id.

**Example Entity**:
```csharp
public class TestEntity : IHasId<int>
{
    public int Id { get; set; }
}
```

### ISoftDelete
An entity that will soft delete. (Delete without removing from the database)

**Example Entity**:
```csharp
public class TestEntity : ISoftDelete
{
    public DateTime? DeletedAt { get; set; } = null;
}
```


### IHardDelete
An entity that will hard delete. (Delete and remove from the database)

**Example Entity**:
```csharp
public class TestEntity : IHardDelete
{
    
}
```

### INoUpdate
An entity that will not update. (Entity will Error if An Update is attempted)

**Example Entity**:
```csharp
public class TestEntity : INoUpdate
{
    
}
```

### IAppendUpdate
An entity that will soft delete current entity and append a new entity with the changes.

**Example Entity**:
```csharp
public class TestEntity : IAppendUpdate
{
    public int Id { get; set; }
    public string? Text { get; set; } // This is example property, you can have any property you want
    public DateTime? DeletedAt { get; set; }
    public int OriginId { get; set; }
    public IAppendUpdate<int> Copy() // Copy is required to create a new entity with the changes
    {
        return new TestEntityAppendUpdate
        {
            Id = Id,
            Text = Text,
            OriginId = Id
        };
    }
}
```

### IOwnedEntity
An entity that is owned by an Identity User. (only that user can access the entity)

**Example Entity**:
```csharp
public class TestEntity : IOwnedEntity
{
    public string? OwnerId { get; set; }
}
```

