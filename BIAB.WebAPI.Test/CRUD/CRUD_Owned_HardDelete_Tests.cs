using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.Test.CRUD;

using BIAB.WebAPI.CRUD;

public class CRUD_Owned_HardDelete_Tests
{
    private string randomId = Guid.NewGuid().ToString();
    private Repository<CRUDTestDbContext, OwnedHardDeleteEntity, int> _repository;
    private string otherRandomId = Guid.NewGuid().ToString();
    private Repository<CRUDTestDbContext, OwnedHardDeleteEntity, int> _otherUserRepository;
    
    
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CRUDTestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        var context = new CRUDTestDbContext(options);
        _repository = new Repository<CRUDTestDbContext,  OwnedHardDeleteEntity, int>(context, context.OwnedHardDeleteEntities, randomId);
        _otherUserRepository = new Repository<CRUDTestDbContext, OwnedHardDeleteEntity, int>(context, context.OwnedHardDeleteEntities, otherRandomId);
    }

    // Test Get and Get Other
    [Test]
    public void Get_ShouldWork()
    {
        // Arrange
        var entity = new OwnedHardDeleteEntity();
        _repository.Create(entity);
        _repository.SaveChanges();
        
        // Act
        var result = _repository.Get().First(x=>x.Id == entity.Id);
        var otherResult = _otherUserRepository.Get().FirstOrDefault(x=>x.Id == entity.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.AreEqual(randomId, result.OwnerId);
        Assert.AreEqual(null, otherResult);
    }
    
    
    [Test]
    public void Create_ShouldWork()
    {
        // Arrange
        var entity = new OwnedHardDeleteEntity();

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
        var entity = new OwnedHardDeleteEntity();
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
        var entity = new OwnedHardDeleteEntity();
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
        var entity = new OwnedHardDeleteEntity();
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
        var entity = new OwnedHardDeleteEntity();
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