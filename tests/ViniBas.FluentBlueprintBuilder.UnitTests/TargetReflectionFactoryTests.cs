/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using ViniBas.FluentBlueprintBuilder.UnitTests.TargetReflectionFactoryTestsConcreteFakes;

namespace ViniBas.FluentBlueprintBuilder.UnitTests;

public sealed class TargetReflectionFactoryTests
{
    [Fact]
    public void InstantiateFromBlueprint_WithSwitableConstructor_ShouldCreateInstanceWithCorrectValues()
    {
        var blueprint = new MixedBlueprint
        {
            PropA = "Test",
            PropB = 30,
            PropC = true,
            PropD = DateTime.MaxValue,
            PropE = 55,
            PropF = 3.14m,
        };
        var factory = new TargetReflectionFactory<MixedTarget>();

        var result = factory.InstantiateFromBlueprint(blueprint);

        Assert.NotNull(result);
        Assert.Equal("Test_suitable_constructor", result.PropA);
        Assert.Equal(30, result.PropB);
        Assert.False(result.PropC);
        Assert.Equal(DateTime.MinValue, result.PropD);
        Assert.Equal(55, result.PropE);
        Assert.Equal(0, result.PropF);
    }


    [Fact]
    public void InstantiateFromBlueprint_WithSDefaultConstructor_ShouldCreateInstanceWithCorrectValues()
    {
        var blueprint = new DefaultConstructorBlueprint
        {
            Name = "John Doe",
            Age = 25
        };;
        var factory = new TargetReflectionFactory<DefaultConstructorTarget>();

        var result = factory.InstantiateFromBlueprint(blueprint);

        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal(25, result.Age);
    }


    [Fact]
    public void InstantiateFromBlueprint_WithNoMatchingConstructor_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var blueprint = new NoMatchBlueprint
        {
            Name = "Jane Doe",
            Age = 28
        };
        var factory = new TargetReflectionFactory<NoMatchTarget>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.InstantiateFromBlueprint(blueprint));
        Assert.Contains("No suitable constructor found", exception.Message);
        Assert.Contains(nameof(NoMatchTarget), exception.Message);
        Assert.Contains(nameof(NoMatchBlueprint), exception.Message);
    }
}
