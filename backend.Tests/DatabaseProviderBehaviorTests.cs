using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Models;

namespace backend.Tests;

public class DatabaseProviderBehaviorTests
{
    [Fact]
    public async Task Sqlite_enforces_unique_indexes_that_inmemory_does_not()
    {
        await using var sqliteConnection = new SqliteConnection("DataSource=:memory:");
        await sqliteConnection.OpenAsync();

        var sqliteOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(sqliteConnection)
            .Options;

        await using (var sqliteContext = new AppDbContext(sqliteOptions))
        {
            await sqliteContext.Database.EnsureCreatedAsync();
            sqliteContext.Stores.AddRange(
                new Store { Name = "Samma namn" },
                new Store { Name = "Samma namn" });

            await Assert.ThrowsAsync<DbUpdateException>(() => sqliteContext.SaveChangesAsync());
        }

        var inMemoryOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"inmemory-{Guid.NewGuid():N}")
            .Options;

        await using var inMemoryContext = new AppDbContext(inMemoryOptions);
        await inMemoryContext.Database.EnsureCreatedAsync();
        inMemoryContext.Stores.AddRange(
            new Store { Name = "Samma namn" },
            new Store { Name = "Samma namn" });

        var savedEntities = await inMemoryContext.SaveChangesAsync();

        Assert.Equal(2, savedEntities);
    }
}
