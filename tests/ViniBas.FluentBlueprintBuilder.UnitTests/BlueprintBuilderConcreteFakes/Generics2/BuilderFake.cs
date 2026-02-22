/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Text;

namespace ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics2;

public sealed class BuilderFake : BlueprintBuilder<BuilderFake, TargetFake>
{
    public string Name { get; set; } = "SomeName";
    public StringBuilder Metadata { get; set; } = new("SomeMetadata");
    public byte CustomValueOutsideBlueprint { get; set; } = 10;

    public string DefaultBlueprintNameValue => DefaultBlueprintName;

    public IDictionary<string, Func<BuilderFake>>? ExposedBlueprints { get; private set; }

    protected override void ConfigureBlueprints(IDictionary<string, Func<BuilderFake>> blueprints)
    {
        base.ConfigureBlueprints(blueprints);
        ExposedBlueprints = blueprints;
    }
}
