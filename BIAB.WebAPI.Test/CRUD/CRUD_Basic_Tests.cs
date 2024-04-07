using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.Test.CRUD;

using BIAB.WebAPI.CRUD;

public class CRUD_Basic_Tests
{
    private Repository<CRUDTestDbContext, TestEntity, int> _repository;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CRUDTestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        var context = new CRUDTestDbContext(options);
        _repository = new Repository<CRUDTestDbContext, TestEntity, int>(context, context.TestEntities);
    }

    [Test]
    public void Create_ShouldWork()
    {
        // Arrange
        var entity = new TestEntity();
        
        // Act
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Assert
        var result = _repository.Get().First(x=>x.Id == entity.Id);
        Assert.NotNull(result);
    }
    
    [Test]
    public void Update_ShouldWork()
    {
        // Arrange
        var entity = new TestEntity();
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
    public void Delete_ShouldFail()
    {
        // Arrange
        var entity = new TestEntity();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        try
        {
            _repository.Delete(entity);
            _repository.SaveChanges();
            Assert.Fail();
        }
        catch (Exception)
        {
            // Assert
            var result = _repository.Get().First(x=>x.Id == entity.Id);
            Assert.AreEqual(entity.Id, result.Id);
        }
    }
}