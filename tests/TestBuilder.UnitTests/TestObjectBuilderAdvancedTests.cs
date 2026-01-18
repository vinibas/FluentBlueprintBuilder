/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of TestBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using TestBuilder.UnitTests.ConcreteFakes;

namespace TestBuilder.UnitTests;

public class TestObjectBuilderAdvancedTests
{
    [Fact]
    public void Build_WithoutPresetKey_ShouldUseFirstPreset()
    {
        // Act
        var targetCreated = BuilderFake.Create().Build();

        // Assert
        Assert.Equal(new List<string> { "tag1", "tag2" }, targetCreated.Tags);
        Assert.Equal(new Dictionary<string, object> { ["key1"] = "value1" }, targetCreated.Metadata);
    }

    [Fact]
    public void Build_PassingPresetKey_ShouldUseSpecificPreset()
    {
        // Act
        var targetCreated = BuilderFake.Create("alternative").Build();

        // Assert
        Assert.Equal(new List<string> { "altTag1", "altTag2" }, targetCreated.Tags);
        Assert.Equal(new Dictionary<string, object> { ["altKey1"] = "altValue1" }, targetCreated.Metadata);
    }

    [Fact]
    public void Set_Property_ShouldOverridePresetValue()
    {
        // Arrange
        var builderCreated = BuilderFake.Create();
        var newTagValue = new List<string> { "customTag1", "customTag2" };

        // Act
        var targetCreated = builderCreated
            .Set(b => b.Tags, newTagValue)
            .Build();

        // Assert
        Assert.Equal(newTagValue, targetCreated.Tags);
        Assert.Equal(new Dictionary<string, object> { ["key1"] = "value1" }, targetCreated.Metadata);
    }

    [Fact]
    public void Build_WithInvalidPresetKey_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(() => BuilderFake.Create("invalid_key").Build());
        Assert.Contains("Preset 'invalid_key' not found", exception.Message);
    }
}
