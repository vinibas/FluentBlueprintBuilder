/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of TestBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

namespace TestBuilder.UnitTests.ConcreteFakes.Generics3OverrideGetInstance;

public class BuilderFake : TestObjectBuilder<BuilderFake, BlueprintFake, TargetFake>
{
    protected override void ConfigureBlueprints(IDictionary<string, Func<BlueprintFake>> blueprints)
    {
        blueprints["default"] = () => new BlueprintFake(
            Tags: [ "tag1", "tag2" ],
            Metadata: new Dictionary<string, object> { ["key1"] = "value1" }
        );
        blueprints["alternative"] = () => new BlueprintFake(
            Tags: [ "altTag1", "altTag2" ],
            Metadata: new Dictionary<string, object> { ["altKey1"] = "altValue1" }
        );
    }

    protected override TargetFake GetInstance(BlueprintFake blueprint)
        => new ()
        {
            Tags = blueprint.Tags,
            Metadata = blueprint.Metadata,
        };
}
