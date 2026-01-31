/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Text;

namespace ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics3OverrideGetInstance;

public class BuilderFakeNoOverridingGetInstance : BlueprintBuilder<BuilderFakeNoOverridingGetInstance, BlueprintFake, TargetFake>
{
    protected override void ConfigureBlueprints(IDictionary<string, Func<BlueprintFake>> blueprints)
    {
        blueprints["default"] = () => new BlueprintFake("SomeName", new StringBuilder("SomeMetadata"));
        blueprints["alternative"] = () => new BlueprintFake("AlternativeName", new StringBuilder("AlternativeMetadata"));
    }
}

public class BuilderFakeOverridingGetInstance : BuilderFakeNoOverridingGetInstance
{
    protected override TargetFake GetInstance(BlueprintFake blueprint)
        => new ()
        {
            Name = blueprint.Name,
            Metadata = blueprint.Metadata,
        };
}
