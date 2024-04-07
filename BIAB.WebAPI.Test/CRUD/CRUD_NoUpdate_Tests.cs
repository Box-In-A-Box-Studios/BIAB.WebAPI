using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.Test.CRUD;

using BIAB.WebAPI.CRUD;

public class CRUD_NoUpdate_Tests
{
    private Repository<CRUDTestDbContext, TestEntityNoUpdate, int> _repository;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CRUDTestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        var context = new CRUDTestDbContext(options);
        _repository = new Repository<CRUDTestDbContext, TestEntityNoUpdate, int>(context, context.TestEntitiesNoUpdate);
    }

    // Get Should work but not have tracking
    [Test]
    public void Get_ShouldWork()
    {
        // Arrange
        var entity = new TestEntityNoUpdate();
        entity.Text = "Test";
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        var result = _repository.Get().First(x=>x.Id == entity.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.AreEqual(entity.Text, result.Text);
        
        // Act 2
        result.Text = "Changed";
        _repository.SaveChanges();
        
        // Assert 2
        var result2 = _repository.Get().First(x=>x.Id == entity.Id);
        Assert.AreEqual(entity.Text, result2.Text);
    }
    
    [Test]
    public void Update_ShouldFail()
    {
        // Arrange
        var entity = new TestEntityNoUpdate();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        try {
            entity.Text = "Updated";
            _repository.Update(entity);
            _repository.SaveChanges();
        } catch (Exception e) {
            // Assert
            Assert.Pass();
            return;
        }
        Assert.Fail("No Exception Thrown");
    }
    
    // Delete does not apply to NoUpdate
}