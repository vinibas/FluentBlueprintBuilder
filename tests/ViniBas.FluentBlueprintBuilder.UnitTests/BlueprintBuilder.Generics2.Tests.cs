/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics2;

namespace ViniBas.FluentBlueprintBuilder.UnitTests;

public sealed class BlueprintBuilderGenerics2Tests
{
    private readonly DateTime _defaultDate = new DateTime(2026, 2, 10);

    [Fact]
    public void Build_WithoutBlueprintKey_ShouldUseFirstBlueprint()
    {
        // Act
        var targetCreated = BuilderFake.Create().Build();

        // Assert
        Assert.Equal("SomeName", targetCreated.Name);
        Assert.Equal("SomeMetadata", targetCreated.Metadata.ToString());
        Assert.Equal(_defaultDate, targetCreated.SomeDate);
    }

    [Fact]
    public void Build_PassingBlueprintKey_ShouldUseSpecificBlueprint()
    {
        // Act
        var targetCreated = BuilderFake.Create("default").Build();

        // Assert
        Assert.Equal("SomeName", targetCreated.Name);
        Assert.Equal("SomeMetadata", targetCreated.Metadata.ToString());
        Assert.Equal(_defaultDate, targetCreated.SomeDate);
    }

    [Fact]
    public void Set_Property_ShouldOverrideBlueprintValue()
    {
        // Arrange
        var builderCreated = BuilderFake.Create();
        var newNameValue = "CustomName";

        // Act
        var targetCreated = builderCreated
            .Set(b => b.Name, newNameValue)
            .Build();

        // Assert
        Assert.Equal(newNameValue, targetCreated.Name);
        Assert.Equal("SomeMetadata", targetCreated.Metadata.ToString());
        Assert.Equal(_defaultDate, targetCreated.SomeDate);
    }

    [Fact]
    public void Build_MissingPropertyInBlueprint_WhenRequiredInConstructor_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => BuilderFakeMissingName.Create().Build());
        Assert.Contains("No suitable constructor found", exception.Message);
    }

    [Fact]
    public void Build_MissingPropertyInBlueprint_WhenNotRequiredInConstructor_ShouldKeepInitialValue()
    {
        // Act
        var targetCreated = BuilderFakeMissingMetadata.Create().Build();

        // Assert
        Assert.Equal("SomeName", targetCreated.Name);
        Assert.Equal("InitialMetadata", targetCreated.Metadata.ToString());
        Assert.Equal(_defaultDate, targetCreated.SomeDate);
    }
}
