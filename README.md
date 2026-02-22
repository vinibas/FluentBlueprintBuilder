# ViniBas.FluentBlueprintBuilder

Copyright (c) Vinícius Bastos da Silva 2026.
Licensed under the GNU Lesser General Public License v3 (LGPL v3).
See the `LICENSE.txt` file for details.

# Introduction

ViniBas.FluentBlueprintBuilder is a library that provides an abstract class to assist in easily building objects using a fluent syntax. It supports creating pre-configured states (blueprints), as well as overriding individual values at usage time.

Builders created with this library can be used in various scenarios:
- **Unit tests**: generating fake objects with valid/invalid data scenarios
- **Database seeding**: generating seed data
- **Configuration management**: creating configurations for different environments
- **Domain objects (DDD)**: creating objects with different states
- And more...

# How to use

## Installation

First, you need to install the library in your project as a NuGet package:

```bash
dotnet add package ViniBas.FluentBlueprintBuilder
```

## Creating your Builder class

You must create your builder classes by inheriting from `BlueprintBuilder<>`.

There are two variants of `BlueprintBuilder`:
- `BlueprintBuilder<TBuilder, TBlueprint, TTarget>`
- `BlueprintBuilder<TBuilder, TTarget>` (here, `TBuilder` itself is used as the blueprint)

Expected generic type arguments:

- **TBuilder**: The builder class itself that you are creating.
- **TBlueprint**: A class that will contain the values used to create your final target object. These values can vary per scenario and/or be modified during builder usage. If you use `BlueprintBuilder<TBuilder, TTarget>`, the builder itself acts as the blueprint.
- **TTarget**: The final type you intend to construct with your builder.

Your builder class that inherits from `BlueprintBuilder<>` must provide a **public parameterless constructor** so the instance can be created internally by the `Create` method.

There are three methods you may override: `ConfigureBlueprints`, `ConfigureDefaultValues`, and `GetInstance`.

### `ConfigureBlueprints`

`ConfigureBlueprints` receives a dictionary as a parameter. You must add at least one blueprint.
A blueprint consists of:
- an identifier key (`string`), and
- a factory function (`Func<TBlueprint>`) that creates the blueprint instance.

Blueprints are selected when calling `Build`, based on the `blueprintKey` or `index` passed to it (see [Using the Builder](#using-the-builder-in-your-code)).

Overriding this method is:
- **Required** for `BlueprintBuilder<TBuilder, TBlueprint, TTarget>`
- **Optional** for `BlueprintBuilder<TBuilder, TTarget>` (its default implementation adds a `"default"` blueprint returning the current `TBuilder` instance; you can reference this key via the `DefaultBlueprintName` constant)

### `ConfigureDefaultValues`

`ConfigureDefaultValues` allows you to define the baseline state for your blueprint properties. This method is called automatically during builder initialization.

Inside this method, you can use the `Set` method to assign default values. This is the ideal place to integrate data generation libraries like **Bogus** to ensure every built object has valid, unique data.

Values defined here:
- Are applied after the blueprint is instantiated.
- Can be overridden by specific `Set` calls in the fluent chain.

### `GetInstance`

`GetInstance` creates the target object (`TTarget`) based on the blueprint instance received as a parameter.

Overriding this method is optional in both variants. The default implementation uses reflection:
- It creates a `TTarget` instance using a compatible constructor.
- If multiple constructors exist, it selects the one with the largest number of matching arguments (by name and compatible type).
- Remaining compatible properties not passed via constructor are assigned via setters.
- Incompatible properties (by name or type) are ignored.

To use the default `GetInstance`, `TTarget` must have at least one constructor whose parameters can be satisfied by blueprint properties (or a parameterless constructor).

If you override `GetInstance`, you can build the target object manually—useful if you want to avoid reflection or if `TTarget` has no compatible constructors.

### Example

Consider that we want to create an instance of the User class as the target:

```csharp
public class User
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}
```

#### Version using `BlueprintBuilder<TBuilder, TBlueprint, TTarget>`

```csharp
using ViniBas.FluentBlueprintBuilder;
using Bogus; // Example library for data generation

public class UserBuilder : BlueprintBuilder<UserBuilder, UserBlueprint, User>
{
    protected override void ConfigureDefaultValues()
    {
        var faker = new Faker();

        // Dynamic default values (generated per build)
        Set(x => x.Name, _ => faker.Name.FullName());
        Set(x => x.Age, _ => faker.Random.Int(18, 80));
        // Dependent value: Email depends on the generated Name
        Set(x => x.Email, bp => faker.Internet.Email(bp.Name));
    }

    protected override void ConfigureBlueprints(IDictionary<string, Func<UserBlueprint>> blueprints)
    {
        blueprints["default"] = () => new UserBlueprint("John Doe", 30, "john@example.com");
        blueprints["admin"] = () => new UserBlueprint("Admin User", 40, "admin@example.com");
        blueprints["minor"] = () => new UserBlueprint("Kid User", 15, "kid@example.com");
    }

    // Optional:
    protected override User GetInstance(UserBlueprint blueprint)
        => new ()
        {
            Name = blueprint.Name,
            Age = blueprint.Age,
            Email = blueprint.Email
        };
}

public record UserBlueprint(string Name, int Age, string Email);
```

#### Version using `BlueprintBuilder<TBuilder, TTarget>`

```csharp
using ViniBas.FluentBlueprintBuilder;

public class UserBuilder : BlueprintBuilder<UserBuilder, User>
{
    public string Name { get; set; } = "John Doe";
    public int Age { get; set; } = 30;
    public string Email { get; set; } = "john@example.com";

    // Optional (the default implementation already registers DefaultBlueprintName => this).
    // Override only if you need to add more blueprints:
    protected override void ConfigureBlueprints(IDictionary<string, Func<UserBuilder>> blueprints)
    {
        base.ConfigureBlueprints(blueprints); // registers "default" => this
        blueprints["admin"] = () => new UserBuilder { Name = "Admin User", Age = 40, Email = "admin@example.com" };
    }

    // Optional:
    protected override User GetInstance(UserBuilder blueprint)
        => new ()
        {
            Name = blueprint.Name,
            Age = blueprint.Age,
            Email = blueprint.Email
        };
}

```

## Using the Builder in your code

The fluent API uses the following methods:

- `Create(defaultBlueprintKey?)` to create a builder instance, optionally specifying a **per-instance default blueprint key**. This key is used as a fallback by `Build` when neither a `blueprintKey` nor an `index` is explicitly passed to it. If none of these are provided, the first registered blueprint is used.
- `Set` (optional) to override blueprint values. It supports:
  - **Static values**: `.Set(p => p.Age, 25)`
  - **Dynamic generators**: `.Set(p => p.Id, _ => Guid.NewGuid())` (useful for unique values per build)
  - **Dependent values**: `.Set(p => p.Email, bp => $"{bp.Name}@example.com")` (value depends on the current blueprint state)
- `Build(blueprintKey?, index?)` to construct the final target object. You can specify either a `blueprintKey` (case-insensitive string; throws `KeyNotFoundException` if not found) or an `index` (zero-based position based on blueprint registration order) to select the desired blueprint. If both are provided and refer to different blueprints, an `ArgumentOutOfRangeException` is thrown. If neither is provided, falls back to the default key set via `Create`, or to the first registered blueprint.
- `BuildMany` to construct a sequence of target objects. It has two overloads:
  - `BuildMany(params string[] blueprintKeys)` — builds one object per provided key, in the given order.
  - `BuildMany(uint? size, params string[] blueprintKeys)` — builds `size` objects, cycling through the provided keys in order. If no keys are given, it cycles through all registered blueprints in registration order. If `size` is omitted, it defaults to the number of provided keys, or to the total number of registered blueprints if no keys are given.

### Example

```csharp
var user = UserBuilder.Create()
    .Set(p => p.Name, "Changed Name")
    // Dynamic value depending on the current state of the blueprint
    .Set(p => p.Email, bp => $"changed.{bp.Name.Replace(" ", ".")}@example.com")
    .Build("my-blueprint-key");
```
