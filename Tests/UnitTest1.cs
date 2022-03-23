using bulkextensions_730.Data;
using EFCore.BulkExtensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bulkextensions_730.Tests;

[TestClass]
public class UnitTest1
{
    #region Fields

    private ApplicationDbContext context;

    #endregion

    #region Init/Cleanup

    [TestInitialize]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=bulkextensions730;Trusted_Connection=True;")
            .Options;

        context = new ApplicationDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (context != null)
            context.Dispose();
    }

    #endregion

    #region Givens

    // Given: A set of parents
    private IEnumerable<Parent> GivenParents(int count)
    {
        return  Enumerable.Range(1,count).Select(x => new Parent() { Name = x.ToString() });
    }

    // Given: A set of parents with varying number of children, where some children
    // are equal to other children 
    private IEnumerable<Parent> GivenParentsWithChildren(int count)
    {
        return Enumerable
            .Range(1,count)
            .Select(x => new Parent() 
            { 
                Name = x.ToString(),
                Children = Enumerable
                    .Range(1,x)
                    .Select(y => new Child() 
                    { 
                        Name = y.ToString(), 
                        Age = y
                    })
                    .ToList()
            })
            .ToList();
    }

    // Given: A set of parents with varying number of children, where all children
    // are completely unique 
    private IEnumerable<Parent> GivenParentsWithUniqueChildren(int count)
    {
        int age = 1;
        return Enumerable
            .Range(1,count)
            .Select(x => new Parent() 
            { 
                Name = x.ToString(),
                Children = Enumerable
                    .Range(1,x)
                    .Select(y => new Child() 
                    { 
                        Name = y.ToString(), 
                        Age = age++
                    })
                    .ToList()
            })
            .ToList();
    }

    #endregion

    #region Tests

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

    [TestMethod]
    public void AddParents()
    {
        // Given: A set of parents
        var count = 25;
        var parents = GivenParents(count);

        // When: Adding them to the database
        context.Set<Parent>().AddRange(parents);
        context.SaveChanges();

        // Then: They are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);
    }

    [TestMethod]
    public void AddParentsWithChildren()
    {
        // Given: A set of parents with varying number of children
        var count = 25;
        var parents = GivenParentsWithChildren(count);
        var numchildren = parents.Sum(x=>x.Children.Count);

        // When: Adding them to the database
        context.Set<Parent>().AddRange(parents);
        context.SaveChanges();

        // Then: They are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);

        // And: The children are separately in the database as well
        var childrencount = context.Set<Child>().Count();
        Assert.AreEqual(numchildren,childrencount);
    }

    [TestMethod, Priority(2)]
    public void BulkAddParentsWithChildren()
    {
        // Given: A set of parents with varying number of children, where some children
        // are equal to other children 
        var count = 25;
        var parents = GivenParentsWithChildren(count).ToList();
        var numchildren = parents.Sum(x=>x.Children.Count);

        // When: Adding them to the database (using bulk extensions)
        context.BulkInsert(parents,b => b.IncludeGraph = true);

        // Then: They are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);

        // And: The children are separately in the database as well
        var childrencount = context.Set<Child>().Count();
        Assert.AreEqual(numchildren,childrencount);
    }

    [TestMethod, Priority(1)]
    public void BulkAddParentsWithUniqueChildren()
    {
        // Given: A set of parents with varying number of children, where some children
        // are equal to other children 
        var count = 25;
        var parents = GivenParentsWithUniqueChildren(count).ToList();
        var numchildren = parents.Sum(x=>x.Children.Count);

        // When: Adding them to the database (using bulk extensions)
        context.BulkInsert(parents,b => b.IncludeGraph = true);

        // Then: They are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);

        // And: The children are separately in the database as well
        var childrencount = context.Set<Child>().Count();
        Assert.AreEqual(numchildren,childrencount);
    }

    #endregion
}