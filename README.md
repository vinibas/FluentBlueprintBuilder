# ViniBas.FluentBlueprintBuilder

Copyright (c) Vinícius Bastos da Silva 2026
Licensed under the GNU Lesser General Public License v3 (LGPL v3).
See the LICENSE.txt file for details.

# Introduction

ViniBas.FluentBlueprintBuilder is a library that provides an abstract class to assist in building fake objects for testing using a fluent syntax. It allows for the creation of pre-configured states, as well as individual value configuration during usage.

# How to use

First, you need to install the library in your test project as a NuGet package:

`dotnet add package ViniBas.FluentBlueprintBuilder`

## Creating your Builder class

You must create your builder classes by inheriting from `TestObjectBuilder<TBuilder, TBlueprint, TTarget>`, where the type arguments are:
- **TBuilder**: The builder class itself that you are creating;
- **TBlueprint**: A class that will contain the values used to create your final class. These values can vary according to scenarios or be modified during the creation of your builder;
- **TTarget**: The type of the object you intend to create at the end with your builder object.

When inheriting from `TestObjectBuilder`, you must provide a public parameterless constructor so that the instance can be created internally by the `Create` method. You also need to implement the abstract methods `ConfigureBlueprints` and `GetInstance`.

In the `ConfigureBlueprints` method, you will receive a dictionary instance as a parameter, and you must add at least one Blueprint. A Blueprint consists of an identifier key and a creation `Func` for your Blueprint. These blueprints will be used according to the `blueprintKey` passed in the `Create` method.

In the `GetInstance` method, you will create your final class based on the values of the Blueprint object received as a parameter.

### Example

```csharp
public class SampleBuilder : TestObjectBuilder<SampleBuilder, SampleBlueprint, SampleTarget>
{
    protected override void ConfigureBlueprints(IDictionary<string, Func<SampleBlueprint>> blueprints)
        => blueprints["sample"] = () => new SampleBlueprint("Sample Name", 30, "Sample Nickname");

    protected override SampleTarget GetInstance(SampleBlueprint blueprint)
        => new ()
        {
            Name = blueprint.Name,
            Age = blueprint.Age,
            Nickname = blueprint.Nickname
        };
}

public record SampleBlueprint(string Name, int Age, string Nickname);

public class SampleTarget
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Nickname { get; set; } = string.Empty;
}
```

### Tip

If you do not want to create a separate class for the Blueprint, you can use the Builder class itself instead. See an example below:

```csharp
public class SampleBuilder : TestObjectBuilder<SampleBuilder, SampleBuilder, SampleTarget>
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Nickname { get; set; } = string.Empty;

    private SampleBuilder SetValues(string name, int age, string nickname)
    {
        Name = name;
        Age = age;
        Nickname = nickname;
        
        return this;
    }

    protected override void ConfigureBlueprints(IDictionary<string, Func<SampleBuilder>> blueprints)
        => blueprints["sample"] = () => SetValues("Sample Name", 30, "SampleNick");

    protected override SampleTarget GetInstance(SampleBuilder blueprint)
        => new ()
        {
            Name = blueprint.Name,
            Age = blueprint.Age,
            Nickname = blueprint.Nickname
        };
}

public class SampleTarget
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Nickname { get; set; } = string.Empty;
}
```

## Using the Builder in your code

Using the Builder class is very simple, utilizing a fluent language via 3 methods: `Create`, to create the builder object passing the desired Blueprint; `Set`, to change values in your blueprint using lambda expressions; and `Build` to construct your final object.

### Example

```csharp
var myTargetObject = SampleBuilder.Create("sample")
    .Set(p => p.Name, "Changed Name")
    .Set(p => p.Nickname, "Changed Nickname")
    .Build();
```
