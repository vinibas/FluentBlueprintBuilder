/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Text;

namespace ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics2OriginalGetInstance;

public sealed class BuilderFake : BlueprintBuilder<BuilderFake, TargetFake>
{
    public string Name { get; set; } = "SomeName";
    public StringBuilder Metadata { get; set; } = new("SomeMetadata");
    public byte CustomValueOutsideBlueprint { get; set; } = 10;
}

public sealed class BuilderFakeMissingName : BlueprintBuilder<BuilderFakeMissingName, TargetFake>
{
    public StringBuilder Metadata { get; set; } = new("SomeMetadata");
    public byte CustomValueOutsideBlueprint { get; set; } = 10;
}

public sealed class BuilderFakeMissingMetadata : BlueprintBuilder<BuilderFakeMissingMetadata, TargetFake>
{
    public string Name { get; set; } = "SomeName";
    public byte CustomValueOutsideBlueprint { get; set; } = 10;
}
