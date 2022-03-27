# BulkInsertAsync with IncludeGraph does not insert multiple equal subentities

This is a repro for [Issue #780](https://github.com/borisdj/EFCore.BulkExtensions/issues/780) on [EFCore.BulkExtensions](https://github.com/borisdj/EFCore.BulkExtensions). (Sorry for any confusion, I dyslexically named this repo "730" when it should have been "780".)

[![Build](https://github.com/jcoliz/bulkextensions-730/actions/workflows/build.yml/badge.svg)](https://github.com/jcoliz/bulkextensions-730/actions/workflows/build.yml)

## Issue Description

### Given: A set of parent entities with varying number of children entities, where some children are equal to other children 

This uses a pair of entities, a Parent which contains multiple Children. See the entities defined in [Entities.cs](https://github.com/jcoliz/bulkextensions-730/blob/main/Data/Entities.cs). The driving factor in this issue is that Children have Equals(object) and GetHashCode() defined which exclude the ID.

```C#
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

    [TestMethod]
    public void BulkAddParentsWithChildren()
    {
        // Given: A set of parents with varying number of children, where some children
        // are equal to other children 
        var count = 25;
        var parents = GivenParentsWithChildren(count).ToList();
        var numchildren = parents.Sum(x=>x.Children.Count);
```

### When: Adding the parents (with contained children)

```C#
        // When: Adding the parents to the database (using bulk extensions)
        context.BulkInsert(parents,b => b.IncludeGraph = true);
```

### Then: The children should all be in the database

```C#
        // Then: The children are separately in the database as well
        var childrencount = context.Set<Child>().Count();
        Assert.AreEqual(numchildren,childrencount);
    }
```

Unfortunately, this fails on EFCore.BulkExtensions 6.4.2.

```
  Failed BulkAddParentsWithChildren [1 s]
  Error Message:
   Assert.AreEqual failed. Expected:<325>. Actual:<25>.
  Stack Trace:
     at bulkextensions_730.Tests.UnitTest1.BulkAddParentsWithChildren() in C:\Source\jcoliz\bulkextensions-730\Tests\UnitTest1.cs:line 172
```

Note that if all the children are unique, where no child of any parent is equal to any child of any other parent, then the operation will
work successfully. See the "BulkAddParentsWithUniqueChildren" test.

Also, inserting the parents with non-unique children using EFCore's DbSet.AddRange works correctly. See the "AddParentsWithChildren" test.

### Workaround: Bulk insert the parents and children separately

The workaround I'm using now is to separately add the parents and children as two separate
bulk insert operations. This requires that the children have their parent ID set at the time
of insert, so it requires a coding step in between

First, we construct the data slightly differently, such that the child has a link up to its parent

```c#
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
```

Insert the parents with SetOutputIdentity:

```c#
        // When: Adding the parents to the database (using bulk extensions, asking for identities to be set)
        // Note that we need the identities set for the next stage
        context.BulkInsert(parents,b => { b.SetOutputIdentity = true; });
```

Set the parent ID's on each child:

```c#
        // And: Populating the children with their parents' ID
        foreach(var child in parents.SelectMany(x=>x.Children))
            child.ParentID = child.Parent.ID;
```

Then, finally insert the children as well:

```c#
        // And: Adding the children to the database
        context.BulkInsert(parents.SelectMany(x=>x.Children).ToList());
```

See in in action with the "BulkAddParentsWithChildrenSeparately" test.

```Powershell
PS bulkextensions-730> dotnet test --filter BulkAddParentsWithChildrenSeparately

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 1 s - bulkextensions-730.dll (net6.0)
```

## Using the repro

### Clone it

```Powershell
PS> git clone https://github.com/jcoliz/bulkextensions-730.git
Cloning into 'bulkextensions-730'...
remote: Enumerating objects: 64, done.
remote: Counting objects: 100% (64/64), done.
remote: Compressing objects: 100% (30/30), done.
Receiving objects: 100% (64/64), 21.12 KiB | 322.00 KiB/s, done.
Resolving deltas: 100% (24/24), done.

PS> cd .\bulkextensions-730\
```

### Run passing tests

Run with "priority=1" filter, which will run only the passing tests

```Powershell
PS bulkextensions-730> dotnet test --filter priority=1

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 3 s - bulkextensions-730.dll (net6.0)
```

### Run failing test

Run with "priority=2" filter to run the failing test

```Powershell
PS bulkextensions-730> dotnet test --filter priority=2

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

  Failed BulkAddParentsWithChildren [1 s]
  Error Message:
   Assert.AreEqual failed. Expected:<325>. Actual:<25>.
  Stack Trace:
     at bulkextensions_730.Tests.UnitTest1.BulkAddParentsWithChildren() in bulkextensions-730\Tests\UnitTest1.cs:line 172

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 1 s - bulkextensions-730.dll (net6.0)
```