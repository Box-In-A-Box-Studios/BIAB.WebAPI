using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.Test.CRUD;

using BIAB.WebAPI.CRUD;

public class CRUD_AppendUpdate_Tests
{
    private Repository<CRUDTestDbContext, TestEntityAppendUpdate, int> _repository;
    
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
        
        _repository = new Repository<CRUDTestDbContext, TestEntityAppendUpdate, int>(context, context.TestEntitiesAppendUpdate);
    }

    [Test]
    public void Create_ShouldWork()
    {
        // Arrange
        var entity = new TestEntityAppendUpdate();

        // Act
        _repository.Create(entity);
        _repository.SaveChanges();

        // Assert
        var result = _repository.Get().First(x => x.Id == entity.Id);
        Assert.NotNull(result);
    }
    
    [Test]
    public void Update_ShouldWork()
    {
        // Arrange
        var entity = new TestEntityAppendUpdate();
        _repository.Create(entity);
        _repository.SaveChanges();
        var id = entity.Id;
        
        // Act
        entity.Text = "Hello, World!";
        _repository.Update(entity);
        _repository.SaveChanges();
        
        // Assert
        var original = _repository.Get().FirstOrDefault(x=>x.Id == id);
        Assert.IsNull(original);
        
        var original2 = _repository.GetRaw().FirstOrDefault(x=>x.Id == id);
        Assert.NotNull(original2);
        Assert.AreEqual(entity.Id, original2.Id);
        Assert.AreEqual(null, original2.Text);
        
        
        var appended = _repository.Get().First(x=>x.OriginId == entity.Id);
        Assert.AreNotEqual(entity.Id, appended.Id);
        Assert.AreEqual("Hello, World!", appended.Text);
    }
    
    [Test]
    public void Delete_ShouldWork()
    {
        // Arrange
        var entity = new TestEntityAppendUpdate();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        _repository.Delete(entity);
        _repository.SaveChanges();
        
        // Assert
        var result = _repository.Get().FirstOrDefault(x=>x.Id == entity.Id);
        Assert.AreEqual(null, result);
    }

}