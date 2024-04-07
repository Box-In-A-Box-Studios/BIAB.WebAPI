using Microsoft.EntityFrameworkCore;

namespace BIAB.WebAPI.Test.CRUD;

public class CRUDTestDbContext : DbContext
{
    public CRUDTestDbContext(DbContextOptions<CRUDTestDbContext> options) : base(options) { }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    
    public DbSet<TestEntityWithHardDelete> TestEntitiesWithHardDelete => Set<TestEntityWithHardDelete>();
    public DbSet<TestEntityWithSoftDelete> TestEntitiesWithSoftDelete => Set<TestEntityWithSoftDelete>();
    public DbSet<TestEntityAppendUpdate> TestEntitiesAppendUpdate => Set<TestEntityAppendUpdate>();
    
    public DbSet<OwnedHardDeleteEntity> OwnedHardDeleteEntities => Set<OwnedHardDeleteEntity>();
    public DbSet<OwnedSoftDeleteEntity> OwnedSoftDeleteEntities => Set<OwnedSoftDeleteEntity>();
    public DbSet<OwnedAppendUpdateEntity> OwnedAppendUpdateEntities => Set<OwnedAppendUpdateEntity>();
}