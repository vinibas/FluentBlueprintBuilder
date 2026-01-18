/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of TestBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

namespace TestBuilder.UnitTests.ConcreteFakes;

public class BuilderFake : TestObjectBuilder<BuilderFake, PresetFake, TargetFake>
{
    protected override void ConfigurePresets(IDictionary<string, Func<PresetFake>> presets)
    {
        presets["default"] = () => new PresetFake(
            Tags: [ "tag1", "tag2" ],
            Metadata: new Dictionary<string, object> { ["key1"] = "value1" }
        );
        presets["alternative"] = () => new PresetFake(
            Tags: [ "altTag1", "altTag2" ],
            Metadata: new Dictionary<string, object> { ["altKey1"] = "altValue1" }
        );
    }

    protected override TargetFake GetInstance(PresetFake preset)
        => new ()
        {
            Tags = preset.Tags,
            Metadata = preset.Metadata,
        };
}
