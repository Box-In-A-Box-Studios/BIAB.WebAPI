using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.Test.CRUD;

using BIAB.WebAPI.CRUD;

public class CRUD_Owned_SoftDelete_Tests
{
    private string randomId = Guid.NewGuid().ToString();
    private Repository<CRUDTestDbContext, OwnedSoftDeleteEntity, int> _repository;
    private string otherRandomId = Guid.NewGuid().ToString();
    private Repository<CRUDTestDbContext, OwnedSoftDeleteEntity, int> _otherUserRepository;
    
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CRUDTestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        var context = new CRUDTestDbContext(options);
        _repository = new Repository<CRUDTestDbContext, OwnedSoftDeleteEntity, int>(context, context.OwnedSoftDeleteEntities, randomId);
        _otherUserRepository = new Repository<CRUDTestDbContext, OwnedSoftDeleteEntity, int>(context, context.OwnedSoftDeleteEntities, otherRandomId);
    }
    
    
    [Test]
    public void Create_ShouldWork()
    {
        // Arrange
        var entity = new OwnedSoftDeleteEntity();

        // Act
        _repository.Create(entity);
        _repository.SaveChanges();

        // Assert
        var result = _repository.Get().First(x => x.Id == entity.Id);
        Assert.NotNull(result);
        Assert.AreEqual(randomId, result.OwnerId);
    }
    
    [Test]
    public void Update_ShouldWork()
    {
        // Arrange
        var entity = new OwnedSoftDeleteEntity();
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
        Assert.AreEqual(randomId, result.OwnerId);
    }
    
    [Test]
    public void Delete_ShouldWork()
    {
        // Arrange
        var entity = new OwnedSoftDeleteEntity();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        _repository.Delete(entity);
        _repository.SaveChanges();
        
        // Assert
        var result = _repository.Get().FirstOrDefault(x=>x.Id == entity.Id);
        Assert.AreEqual(null, result);
    }
    
    [Test]
    public void DeleteOtherUser_ShouldThrow()
    {
        // Arrange
        var entity = new OwnedSoftDeleteEntity();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        try
        {
            _otherUserRepository.Delete(entity);
            _otherUserRepository.SaveChanges();
        }
        catch (UnauthorizedAccessException)
        {
            // Assert
            Assert.Pass();
            return;
        }
        
        Assert.Fail();
    }
    
    [Test]
    public void UpdateOtherUser_ShouldThrow()
    {
        // Arrange
        var entity = new OwnedSoftDeleteEntity();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        try
        {
            entity.Text = "Hello, World!";
            _otherUserRepository.Update(entity);
            _otherUserRepository.SaveChanges();
        }
        catch (UnauthorizedAccessException)
        {
            // Assert
            Assert.Pass();
            return;
        }
        
        Assert.Fail();
    }

}