using System;
using System.Collections.Generic;

namespace bulkextensions_730.Data;

public class Parent
{
    public int ID { get; set; }

    public string Name { get; set; }

    public ICollection<Child> Children { get; set; }

    public ICollection<Pet> Pets { get; set; }
}

public class Child
{
    public int ID { get; set; }

    public string Name { get; set; }

    public int Age { get; set; }

    public Parent Parent { get; set; }

    public int ParentID { get; set; }

    public override bool Equals(object obj)
    {
        return obj is Child child &&
                Age == child.Age &&
                Name == child.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Age);
    }
}

public class Pet
{
    public int ID { get; set; }

    public string Name { get; set; }
}