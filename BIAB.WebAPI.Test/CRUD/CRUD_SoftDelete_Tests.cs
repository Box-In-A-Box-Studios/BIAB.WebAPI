using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.Test.CRUD;

using BIAB.WebAPI.CRUD;

public class CRUD_SoftDelete_Tests
{
    private Repository<CRUDTestDbContext, TestEntityWithSoftDelete, int> _repository;
    
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CRUDTestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        var context = new CRUDTestDbContext(options);
        _repository = new Repository<CRUDTestDbContext, TestEntityWithSoftDelete, int>(context, context.TestEntitiesWithSoftDelete);
    }

    [Test]
    public void Create_ShouldWork()
    {
        // Arrange
        var entity = new TestEntityWithSoftDelete();

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
        var entity = new TestEntityWithSoftDelete();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        entity.Text = "Hello, World!";
        _repository.Update(entity);
        _repository.SaveChanges();
        
        // Assert
        var result = _repository.Get().First(x=>x.Id == entity.Id);
        Assert.AreEqual(entity.Id, result.Id);
        Assert.AreEqual("Hello, World!", result.Text);
    }
    
    [Test]
    public void Delete_ShouldWork()
    {
        // Arrange
        var entity = new TestEntityWithSoftDelete();
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