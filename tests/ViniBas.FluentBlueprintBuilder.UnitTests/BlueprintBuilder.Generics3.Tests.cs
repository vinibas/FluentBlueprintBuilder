/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Text;
using Moq;
using ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics3;

namespace ViniBas.FluentBlueprintBuilder.UnitTests;

public sealed class BlueprintBuilderGenerics3Tests
{
    [Fact]
    public void Build_WithoutBlueprintKey_ShouldUseFirstBlueprint()
    {
        // Act
        var targetCreated = BuilderFakeOverridingGetInstance.Create().Build();

        // Assert
        Assert.Equal("SomeName", targetCreated.Name);
        Assert.Equal("SomeMetadata", targetCreated.Metadata.ToString());
    }

    [Fact]
    public void Build_WithDefaultBlueprintKeySetAtCreate_ShouldUseSpecificBlueprint()
    {
        // Act
        var targetCreated = BuilderFakeOverridingGetInstance.Create("alternative").Build();

        // Assert
        Assert.Equal("AlternativeName", targetCreated.Name);
        Assert.Equal("AlternativeMetadata", targetCreated.Metadata.ToString());
    }

    [Fact]
    public void Build_PassingBlueprintKeyToMethod_ShouldUseSpecificBlueprint()
    {
        // Act
        var targetCreated = BuilderFakeOverridingGetInstance.Create().Build("alternative");

        // Assert
        Assert.Equal("AlternativeName", targetCreated.Name);
        Assert.Equal("AlternativeMetadata", targetCreated.Metadata.ToString());
    }

    [Fact]
    public void Set_Property_ShouldOverrideBlueprintValue()
    {
        // Arrange
        var builderCreated = BuilderFakeOverridingGetInstance.Create();
        var newNameValue = "CustomName";

        // Act
        var targetCreated = builderCreated
            .Set(b => b.Name, newNameValue)
            .Build();

        // Assert
        Assert.Equal(newNameValue, targetCreated.Name);
        Assert.Equal("SomeMetadata", targetCreated.Metadata.ToString());
    }

    [Fact]
    public void Build_WithInvalidBlueprintKey_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(() => BuilderFakeOverridingGetInstance.Create("invalid_key").Build());
        Assert.Contains("Blueprint 'invalid_key' not found", exception.Message);
    }

    [Fact]
    public void Build_WhenGetInstanceIsNotOverridden_ShouldUseBaseImplementation()
    {
        // Arrange
        var targetCreated = BuilderFakeNoOverridingGetInstance.Create();
        var expectedTarget = new TargetFake
        {
            Name = "SomeName",
            Metadata = new StringBuilder("SomeMetadata"),
        };

        var factoryInstanceMock = new Mock<ITargetReflectionFactory<TargetFake>>();
        factoryInstanceMock.Setup(f => f.InstantiateFromBlueprint(It.IsAny<BlueprintFake>()))
            .Returns(expectedTarget);

        targetCreated._targetFactoryInstance = factoryInstanceMock.Object;

        // Act
        var targetCreatedResult = targetCreated.Build();

        // Assert
        Assert.Same(expectedTarget, targetCreatedResult);
        factoryInstanceMock.Verify(f => f.InstantiateFromBlueprint(It.IsAny<BlueprintFake>()), Times.Once);
    }
}
