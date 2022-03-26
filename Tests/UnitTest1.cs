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
            });
    }

    // Given: A set of parents with varying number of children, where some children
    // are equal to other children, and where the children have a link to their
    // parents
    private IEnumerable<Parent> GivenParentsWithChildrenBacklinked(int count)
    {
        return Enumerable
            .Range(1,count)
            .Select(x =>
            {
                var p = new Parent() 
                { 
                    Name = x.ToString(),
                };

                p.Children = Enumerable
                    .Range(1,x)
                    .Select(y => new Child() 
                    { 
                        Name = y.ToString(), 
                        Age = y,
                        Parent = p
                    })
                    .ToList();

                return p; 
            });
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
            });
    }

    // Given: A set of parents with varying number of pets
    private IEnumerable<Parent> GivenParentsWithPets(int count)
    {
        return Enumerable
            .Range(1,count)
            .Select(x => new Parent() 
            { 
                Name = x.ToString(),
                Pets = Enumerable
                    .Range(1,x)
                    .Select(y => new Pet() 
                    { 
                        Name = y.ToString(), 
                    })
                    .ToList()
            });
    }

    #endregion

    #region Tests

    [TestMethod, Priority(1)]
    public void ChildEquals()
    {
        var one = new Child() { ID = 1, Name = "Samir", Age = 12 };
        var two = new Child() { ID = 2, Name = "Samir", Age = 12 };

        Assert.AreEqual(one,two);
    }

    [TestMethod, Priority(1)]
    public void ChildNotEquals()
    {
        var one = new Child() { ID = 1, Name = "Samir", Age = 12 };
        var two = new Child() { ID = 1, Name = "Samir", Age = 11 };

        Assert.AreNotEqual(one,two);
    }

    [TestMethod, Priority(1)]
    public void AddParents()
    {
        // Given: A set of parents
        var count = 25;
        var parents = GivenParents(count);

        // When: Adding the parents to the database
        context.Set<Parent>().AddRange(parents);
        context.SaveChanges();

        // Then: The parents are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);
    }

    [TestMethod, Priority(1)]
    public void AddParentsWithChildren()
    {
        // Given: A set of parents with varying number of children
        var count = 25;
        var parents = GivenParentsWithChildren(count);
        var numchildren = parents.Sum(x=>x.Children.Count);

        // When: Adding the parents to the database
        context.Set<Parent>().AddRange(parents);
        context.SaveChanges();

        // Then: The parents are in the database
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

        // When: Adding the parents to the database (using bulk extensions)
        context.BulkInsert(parents,b => b.IncludeGraph = true);

        // Then: The parents are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);

        // And: The children are separately in the database as well
        var childrencount = context.Set<Child>().Count();
        Assert.AreEqual(numchildren,childrencount);
    }

    [TestMethod, Priority(1)]
    public void BulkAddParentsWithPets()
    {
        // Given: A set of parents with varying number of children, where some children
        // are equal to other children 
        var count = 25;
        var parents = GivenParentsWithPets(count).ToList();
        var numpets = parents.Sum(x=>x.Pets.Count);

        // When: Adding the parents to the database (using bulk extensions)
        context.BulkInsert(parents,b => b.IncludeGraph = true);

        // Then: The parents are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);

        // And: The pets are separately in the database as well
        var petscount = context.Set<Pet>().Count();
        Assert.AreEqual(numpets,petscount);
    }

    [TestMethod, Priority(1)]
    public void BulkAddParentsWithUniqueChildren()
    {
        // Given: A set of parents with varying number of children, where some children
        // are equal to other children 
        var count = 25;
        var parents = GivenParentsWithUniqueChildren(count).ToList();
        var numchildren = parents.Sum(x=>x.Children.Count);

        // When: Adding the parents to the database (using bulk extensions)
        context.BulkInsert(parents,b => { b.IncludeGraph = true; b.OmitClauseExistsExcept = true; });

        // Then: The parents are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);

        // And: The children are separately in the database as well
        var childrencount = context.Set<Child>().Count();
        Assert.AreEqual(numchildren,childrencount);
    }

    [TestMethod, Priority(1)]
    public void BulkAddParentsWithChildrenSeparately()
    {
        // Given: A set of parents with varying number of children, where some children
        // are equal to other children, and where the children have a link to their
        // parents
        var count = 25;
        var parents = GivenParentsWithChildrenBacklinked(count).ToList();
        var numchildren = parents.Sum(x=>x.Children.Count);

        // When: Adding the parents to the database (using bulk extensions, asking for identities to be set)
        // Note that we need the identities set for the next stage
        context.BulkInsert(parents,b => { b.SetOutputIdentity = true; });

        // And: Populating the children with their parents' ID
        foreach(var child in parents.SelectMany(x=>x.Children))
            child.ParentID = child.Parent.ID;

        // And: Adding the children to the database
        context.BulkInsert(parents.SelectMany(x=>x.Children).ToList());

        // Then: The parents are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);

        // And: The children are separately in the database as well
        var childrencount = context.Set<Child>().Count();
        Assert.AreEqual(numchildren,childrencount);

        // And: Getting a parent includes its children
        var index = 13;
        var found = context.Set<Parent>().Include(x=>x.Children).Where(x=>x.Name == index.ToString()).AsNoTracking().ToList();
        Assert.AreEqual(1,found.Count);
        Assert.AreEqual(index,found.Single().Children.Count);
    }

    [TestMethod, Priority(1)]
    public void AddParentsViaBulk()
    {
        // Given: A set of parents
        var count = 25;
        var parents = GivenParents(count).ToList();

        // When: Adding the parents to the database (using bulk extensions, asking for identities to be set)
        context.BulkInsert(parents,b => { b.SetOutputIdentity = true; });

        // Then: The parents are in the database
        var actual = context.Set<Parent>().Count();
        Assert.AreEqual(count,actual);

        // And: The IDs are populated correctly
        var index = 13;
        var populatedid = parents.Where(x=>x.Name == index.ToString()).Single().ID;
        var dbid = context.Set<Parent>().Where(x=>x.Name == index.ToString()).Single().ID;
        Assert.AreEqual(dbid,populatedid);
    }


    #endregion
}