using bulkextensions_730.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace bulkextensions_730.Tests;

[TestClass]
public class UnitTest1
{
    private SqliteConnection connection;
    private ApplicationDbContext context;

    [TestInitialize]
    public async Task Setup()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection) // Set the connection explicitly, so it won't be closed automatically by EF
            .Options;

        // Create the dabase schema
        // You can use MigrateAsync if you use Migrations
        context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (context != null)
            context.Dispose();
        if (connection != null)
            connection.Dispose();
    }

    [TestMethod]
    public void ChildEquals()
    {
        var one = new Child() { ID = 1, Name = "Samir", Age = 12 };
        var two = new Child() { ID = 2, Name = "Samir", Age = 12 };

        Assert.AreEqual(one,two);
    }

   [TestMethod]
    public void ChildNotEquals()
    {
        var one = new Child() { ID = 1, Name = "Samir", Age = 12 };
        var two = new Child() { ID = 1, Name = "Samir", Age = 11 };

        Assert.AreNotEqual(one,two);
    }
}