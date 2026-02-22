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
    public void Build_WithoutBlueprintKeyOrIndexDefined_ShouldUseFirstBlueprint()
    {
        // Act
        var targetCreated = BuilderFakeOverridingGetInstance.Create().Build();

        // Assert
        Assert.Equal("default", targetCreated.BlueprintKey);
        Assert.Equal("SomeName", targetCreated.Name);
        Assert.Equal("SomeMetadata", targetCreated.Metadata.ToString());
        Assert.Equal(0, targetCreated.Counter);
    }

    [Fact]
    public void Build_WithSomeBlueprintKeyDefined_ShouldUseCorrespondingBlueprint()
    {
        // Act
        var definedOnCreate = BuilderFakeOverridingGetInstance.Create("alternative").Build();
        var definedOnBuild = BuilderFakeOverridingGetInstance.Create().Build("alternative");
        var definedOnBoth = BuilderFakeOverridingGetInstance.Create("alternative2").Build("alternative");

        // Assert
        foreach (var targetCreated in new[] { definedOnCreate, definedOnBuild, definedOnBoth })
        {
            Assert.Equal("alternative", targetCreated.BlueprintKey);
            Assert.Equal("AlternativeName", targetCreated.Name);
            Assert.Equal("AlternativeMetadata", targetCreated.Metadata.ToString());
            Assert.Equal(1, targetCreated.Counter);
        }
    }

    [Fact]
    public void Build_WithIndexDefined_ShouldUseCorrespondingBlueprint()
    {
        // Act
        var targetCreated = BuilderFakeOverridingGetInstance.Create().Build(null, 1);

        // Assert
        Assert.Equal("alternative", targetCreated.BlueprintKey);
        Assert.Equal("AlternativeName", targetCreated.Name);
        Assert.Equal("AlternativeMetadata", targetCreated.Metadata.ToString());
        Assert.Equal(1, targetCreated.Counter);
    }

    [Fact]
    public void Build_WithBlueprintAndIndexNonMatching_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BuilderFakeOverridingGetInstance.Create().Build("alternative", 2));
    }

    [Fact]
    public void Build_WithInvalidBlueprintKey_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        var exception1 = Assert.Throws<KeyNotFoundException>(() => BuilderFakeOverridingGetInstance.Create("invalid_key").Build());
        var exception2 = Assert.Throws<KeyNotFoundException>(() => BuilderFakeOverridingGetInstance.Create().Build("invalid_key"));
        Assert.Contains("Blueprint 'invalid_key' not found", exception1.Message);
        Assert.Contains("Blueprint 'invalid_key' not found", exception2.Message);
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
        var defaultDate = new DateTime(2026, 2, 10);

        // Act
        var targetCreated = BuilderFakeMissingMetadata.Create().Build();

        // Assert
        Assert.Equal("SomeName", targetCreated.Name);
        Assert.Equal("InitialMetadata", targetCreated.Metadata.ToString());
        Assert.Equal(defaultDate, targetCreated.SomeDate);
    }

    [Fact]
    public void BuildMany_PassingMultipleBlueprintKeys_ShouldUseCorrespondingBlueprints()
    {
        // Act
        var targetsCreated = BuilderFakeOverridingGetInstance
            .Create()
            .BuildMany("alternative", "alternative2")
            .ToList();

        Assert.Equal(2, targetsCreated.Count);

        Assert.Equal("alternative", targetsCreated[0].BlueprintKey);
        Assert.Equal("AlternativeName", targetsCreated[0].Name);
        Assert.Equal("AlternativeMetadata", targetsCreated[0].Metadata.ToString());
        Assert.Equal(1, targetsCreated[0].Counter);

        Assert.Equal("alternative2", targetsCreated[1].BlueprintKey);
        Assert.Equal("AlternativeName2", targetsCreated[1].Name);
        Assert.Equal("AlternativeMetadata2", targetsCreated[1].Metadata.ToString());
        Assert.Equal(3, targetsCreated[1].Counter);
    }

    [Fact]
    public void BuildMany_PassingNoParameters_ShouldReturnAEmptyCollection()
    {
        // Act
        var targetsCreated = BuilderFakeOverridingGetInstance
            .Create()
            .BuildMany();

        Assert.Empty(targetsCreated);
    }

    [Fact]
    public void BuildMany_PassingASizeWithoutBlueprintKeys_ShouldReturnACollectionWithThatSize()
    {
        // Act
        var targetsCreated = BuilderFakeOverridingGetInstance
            .Create()
            .BuildMany(6)
            .ToList();

        Assert.Equal(6, targetsCreated.Count);

        for (int i = 0; i < 6; i++)
        {
            Assert.Equal("default", targetsCreated[i].BlueprintKey);
            Assert.Equal("SomeName", targetsCreated[i].Name);
            Assert.Equal("SomeMetadata", targetsCreated[i].Metadata.ToString());
            Assert.Equal(i, targetsCreated[i].Counter);

            i++;

            Assert.Equal("alternative", targetsCreated[i].BlueprintKey);
            Assert.Equal("AlternativeName", targetsCreated[i].Name);
            Assert.Equal("AlternativeMetadata", targetsCreated[i].Metadata.ToString());
            Assert.Equal(i + 1, targetsCreated[i].Counter);

            i++;

            Assert.Equal("alternative2", targetsCreated[i].BlueprintKey);
            Assert.Equal("AlternativeName2", targetsCreated[i].Name);
            Assert.Equal("AlternativeMetadata2", targetsCreated[i].Metadata.ToString());
            Assert.Equal(i + 2, targetsCreated[i].Counter);
        }
    }

    [Fact]
    public void BuildMany_PassingASizeWithBlueprintKeys_ShouldReturnACollectionWithThatSize()
    {
        // Act
        var targetsCreated = BuilderFakeOverridingGetInstance
            .Create()
            .BuildMany(6, "alternative", "alternative2")
            .ToList();

        Assert.Equal(6, targetsCreated.Count);

        for (int i = 0; i < 6; i++)
        {
            Assert.Equal("alternative", targetsCreated[i].BlueprintKey);
            Assert.Equal("AlternativeName", targetsCreated[i].Name);
            Assert.Equal("AlternativeMetadata", targetsCreated[i].Metadata.ToString());
            Assert.Equal(i + 1, targetsCreated[i].Counter);

            i++;

            Assert.Equal("alternative2", targetsCreated[i].BlueprintKey);
            Assert.Equal("AlternativeName2", targetsCreated[i].Name);
            Assert.Equal("AlternativeMetadata2", targetsCreated[i].Metadata.ToString());
            Assert.Equal(i + 2, targetsCreated[i].Counter);
        }
    }

    [Fact]
    public void BuildMany_PassingBlueprintKeysWithoutSize_ShouldReturnACollectionWithOnlyThatBlueprintsOneOfEach()
    {
        // Act
        var targetsCreated = BuilderFakeOverridingGetInstance
            .Create()
            .BuildMany(size: null, "alternative", "alternative2")
            .ToList();

        Assert.Equal(2, targetsCreated.Count);

        Assert.Equal("alternative", targetsCreated[0].BlueprintKey);
        Assert.Equal("AlternativeName", targetsCreated[0].Name);
        Assert.Equal("AlternativeMetadata", targetsCreated[0].Metadata.ToString());
        Assert.Equal(1, targetsCreated[0].Counter);

        Assert.Equal("alternative2", targetsCreated[1].BlueprintKey);
        Assert.Equal("AlternativeName2", targetsCreated[1].Name);
        Assert.Equal("AlternativeMetadata2", targetsCreated[1].Metadata.ToString());
        Assert.Equal(3, targetsCreated[1].Counter);
    }

    [Fact]
    public void Set_WithStaticValue_ShouldOverrideBlueprintValue()
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
        Assert.Equal(0, targetCreated.Counter);
    }

    [Fact]
    public void Set_WithValueGeneratorFunction_ShouldOverrideBlueprintValue()
    {
        // Arrange
        var builderCreated = BuilderFakeOverridingGetInstance
            .Create()
            .Set(b => b.Counter, 5);

        // Act
        var targetCreated = builderCreated
            .Set(b => b.Name, b => $"{b.Name}_{b.Counter}")
            .Build();

        // Assert
        Assert.Equal("SomeName_5", targetCreated.Name);
        Assert.Equal("SomeMetadata", targetCreated.Metadata.ToString());
        Assert.Equal(5, targetCreated.Counter);
    }
}
