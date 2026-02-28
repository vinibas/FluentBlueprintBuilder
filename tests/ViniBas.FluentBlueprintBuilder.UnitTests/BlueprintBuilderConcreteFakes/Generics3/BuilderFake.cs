/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Text;

namespace ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics3;

public class BuilderFakeNoOverridingGetInstance : BlueprintBuilder<BuilderFakeNoOverridingGetInstance, BlueprintFake, TargetFake>
{
    private int Index = 0;

    protected override void ConfigureBlueprints(IDictionary<string, Func<BlueprintFake>> blueprints)
    {
        blueprints["default"] = () => new BlueprintFake("default", "SomeName", new StringBuilder("SomeMetadata"), 0);
        blueprints["alternative"] = () => new BlueprintFake("alternative", "AlternativeName", new StringBuilder("AlternativeMetadata"), 1);
        blueprints["alternative2"] = () => new BlueprintFake("alternative2", "AlternativeName2", new StringBuilder("AlternativeMetadata2"), 2);
    }

    protected override void ConfigureDefaultValues()
    {
        Set(b => b.Counter, b => b.Counter + Index++);
    }

    public IReadOnlyList<string> GetRegisteredBlueprintKeys() => RegisteredBlueprintKeys;
    public BlueprintFake GetBlueprint(string blueprintKey) => CreateBlueprint(blueprintKey);
}

public class BuilderFakeOverridingGetInstance : BuilderFakeNoOverridingGetInstance
{
    protected override TargetFake GetInstance(BlueprintFake blueprint)
        => new ()
        {
            Name = blueprint.Name,
            Metadata = blueprint.Metadata,
            Counter = blueprint.Counter,
        };
}

public class BuilderFakeMissingName : BlueprintBuilder<BuilderFakeMissingName, BlueprintFakeMissingName, TargetFakeWithRequiredConstructor>
{
    protected override void ConfigureBlueprints(IDictionary<string, Func<BlueprintFakeMissingName>> blueprints)
        => blueprints["default"] = () => new BlueprintFakeMissingName(new StringBuilder("SomeMetadata"));
}

public class BuilderFakeMissingMetadata : BlueprintBuilder<BuilderFakeMissingMetadata, BlueprintFakeMissingMetadata, TargetFakeWithRequiredConstructor>
{
    protected override void ConfigureBlueprints(IDictionary<string, Func<BlueprintFakeMissingMetadata>> blueprints)
        => blueprints["default"] = () => new BlueprintFakeMissingMetadata("SomeName");
}
