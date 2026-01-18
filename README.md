# ViniBas.TestBuilder

Copyright (c) Vinícius Bastos da Silva 2026
Licensed under the GNU Lesser General Public License v3 (LGPL v3).
See the LICENSE.txt file for details.

# Introduction

TestBuilder is a library that provides an abstract class to assist in building fake objects for testing using a fluent syntax. It allows for the creation of pre-configured states, as well as individual value configuration during usage.

# How to use

First, you need to install the library in your test project as a NuGet package:

`dotnet add package ViniBas.TestBuilder`

## Creating your Builder class

You must create your builder classes by inheriting from `TestObjectBuilder<T_Builder, T_Presets, T_Target>`, where the type arguments are:
- **T_Builder**: The builder class itself that you are creating;
- **T_Presets**: A class that will contain the values used to create your final class. These values can vary according to scenarios or be modified during the creation of your builder;
- **T_Target**: The type of the object you intend to create at the end with your builder object.

When inheriting from `TestObjectBuilder`, you must provide a public parameterless constructor so that the instance can be created internally by the `Create` method. You also need to implement the abstract methods `ConfigurePresets` and `GetInstance`.

In the `ConfigurePresets` method, you will receive a dictionary instance as a parameter, and you must add at least one Preset. A Preset consists of an identifier key and a creation `Func` for your Preset. These presets will be used according to the `presetKey` passed in the `Create` method.

In the `GetInstance` method, you will create your final class based on the values of the Preset object received as a parameter.

### Example

```csharp
public class SampleBuilder : TestObjectBuilder<SampleBuilder, SamplePreset, SampleTarget>
{
    protected override void ConfigurePresets(IDictionary<string, Func<SamplePreset>> presets)
        => presets["sample"] = () => new SamplePreset("Sample Name", 30, "Sample Nickname");

    protected override SampleTarget GetInstance(SamplePreset preset)
        => new ()
        {
            Name = preset.Name,
            Age = preset.Age,
            Nickname = preset.Nickname
        };
}

public record SamplePreset(string Name, int Age, string Nickname);

public class SampleTarget
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Nickname { get; set; } = string.Empty;
}
```

## Using the Builder in your code

Using the Builder class is very simple, utilizing a fluent language via 3 methods: `Create`, to create the builder object passing the desired Preset; `Set`, to change values in your preset using lambda expressions; and `Build` to construct your final object.

### Example

```csharp
var myTargetObject = SampleBuilder.Create("sample")
    .Set(p => p.Name, "Changed Name")
    .Set(p => p.Nickname, "Changed Nickname")
    .Build();
```
