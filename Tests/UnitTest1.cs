using bulkextensions_730.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace bulkextensions_730.Tests;

[TestClass]
public class UnitTest1
{
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