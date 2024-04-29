using BIAB.WebAPI.CRUD;
using BIAB.WebAPI.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.Test.CRUD;

public class CRUD_Accessor_Tests
{
    // Test the AccessorRepository
    
    private Repository<CRUDTestDbContext, TestEntityWithAccessor, int> _repository;
    private AccessorRepository<CRUDTestDbContext, TestEntityAccessor, TestEntityWithAccessor, int> _accessorRepository;
    private AccessorRepository<CRUDTestDbContext, TestEntityAccessor, TestEntityWithAccessor, int> _accessorRepository2;
    
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CRUDTestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        var context = new CRUDTestDbContext(options);
        
        // Ensure the database is created and empty
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        
        _repository = new Repository<CRUDTestDbContext, TestEntityWithAccessor, int>(context, context.TestEntitiesWithAccessor, "1");
        _accessorRepository = new AccessorRepository<CRUDTestDbContext, TestEntityAccessor, TestEntityWithAccessor, int>(context, context.TestEntitiesAccessor, "1");
        _accessorRepository2 = new AccessorRepository<CRUDTestDbContext, TestEntityAccessor, TestEntityWithAccessor, int>(context, context.TestEntitiesAccessor, "2");
    }
    
    [Test]
    public void AddToEntity()
    {
        // Arrange
        var entity = new TestEntityWithAccessor();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        _accessorRepository.AddToEntity(entity, "2", AccessorType.Viewer);
        _accessorRepository.SaveChanges();
        
        // Assert
        Assert.AreEqual(1, entity.Accessors.Count);
        Assert.AreEqual("2", entity.Accessors[0].OwnerId);
        Assert.AreEqual(AccessorType.Viewer, entity.Accessors[0].AccessorType);
    }
    
    [Test]
    public void RemoveFromEntity()
    {
        // Arrange
        var entity = new TestEntityWithAccessor();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        _accessorRepository.AddToEntity(entity, "2", AccessorType.Viewer);
        _accessorRepository.SaveChanges();
        
        // Act
        _accessorRepository.RemoveFromEntity(entity, "2", AccessorType.Viewer);
        _accessorRepository.SaveChanges();
        
        // Assert
        Assert.AreEqual(0, entity.Accessors.Count);
    }
    
    [Test]
    public void Get()
    {
        // Arrange
        var entity = new TestEntityWithAccessor();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        _accessorRepository.AddToEntity(entity, "2", AccessorType.Viewer);
        _accessorRepository.SaveChanges();
        
        // Act
        var result = _accessorRepository2.Get().ToList();
        
        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("1", result[0].Relation.OwnerId); // Original Owner Still Owns the Entity
        Assert.AreEqual("2", result[0].OwnerId); // Accessor Owner
    }
    
    // Fail to get the entity because the owner is different
    [Test]
    public void GetFail()
    {
        // Arrange
        var entity = new TestEntityWithAccessor();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        var result = _accessorRepository.Get().ToList();
        
        // Assert
        Assert.AreEqual(0, result.Count);
    }
}