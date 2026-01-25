/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

namespace ViniBas.FluentBlueprintBuilder.UnitTests.ConcreteFakes.Generics2OriginalGetInstance;

public class BuilderFake : BlueprintBuilder<BuilderFake, TargetFake>
{
    public List<string> Tags { get; set; } = [ "tag1", "tag2" ];
    public Dictionary<string, object> Metadata { get; set; } =
        new Dictionary<string, object> { ["key1"] = "value1" };
    public byte CustomValueOutsideBlueprint { get; set; } = 10;
}

public class BuilderFakeMissingTag : BlueprintBuilder<BuilderFakeMissingTag, TargetFake>
{
    public Dictionary<string, object> Metadata { get; set; } =
        new Dictionary<string, object> { ["key1"] = "value1" };
    public byte CustomValueOutsideBlueprint { get; set; } = 10;
}

public class BuilderFakeMissingMetadata : BlueprintBuilder<BuilderFakeMissingMetadata, TargetFake>
{
    public List<string> Tags { get; set; } = [ "tag1", "tag2" ];
    public byte CustomValueOutsideBlueprint { get; set; } = 10;
}
