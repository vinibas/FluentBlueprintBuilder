/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of TestBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using TestBuilder.UnitTests.ConcreteFakes.Generics2OriginalGetInstance;

namespace TestBuilder.UnitTests;

public class TestObjectBuilderGenerics2Tests
{
    private readonly DateTime _defaultDate = new DateTime(2026, 2, 10);

    [Fact]
    public void Build_WithoutBlueprintKey_ShouldUseFirstBlueprint()
    {
        // Act
        var targetCreated = BuilderFake.Create().Build();

        // Assert
        Assert.Equal(["tag1", "tag2"], targetCreated.Tags);
        Assert.Equal(new Dictionary<string, object> { ["key1"] = "value1" }, targetCreated.Metadata);
        Assert.Equal(_defaultDate, targetCreated.SomeDate);
    }

    [Fact]
    public void Build_PassingBlueprintKey_ShouldUseSpecificBlueprint()
    {
        // Act
        var targetCreated = BuilderFake.Create("default").Build();

        // Assert
        Assert.Equal(["tag1", "tag2"], targetCreated.Tags);
        Assert.Equal(new Dictionary<string, object> { ["key1"] = "value1" }, targetCreated.Metadata);
        Assert.Equal(_defaultDate, targetCreated.SomeDate);
    }

    [Fact]
    public void Set_Property_ShouldOverrideBlueprintValue()
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
        Assert.Equal(_defaultDate, targetCreated.SomeDate);
    }

    [Fact]
    public void Build_MissingPropertyInBlueprint_WhenRequiredInConstructor_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => BuilderFakeMissingTag.Create().Build());
        Assert.Contains("No suitable constructor found", exception.Message);
    }

    [Fact]
    public void Build_MissingPropertyInBlueprint_WhenNotRequiredInConstructor_ShouldKeepInitialValue()
    {
        // Act
        var targetCreated = BuilderFakeMissingMetadata.Create().Build();

        // Assert
        Assert.Equal(["tag1", "tag2"], targetCreated.Tags);
        Assert.Equal(new Dictionary<string, object> { ["Initialkey"] = "Initial value" }, targetCreated.Metadata);
        Assert.Equal(_defaultDate, targetCreated.SomeDate);
    }
}
