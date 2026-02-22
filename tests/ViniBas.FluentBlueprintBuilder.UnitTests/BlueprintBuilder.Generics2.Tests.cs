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
    [Fact]
    public void ConfigureBlueprints_DefaultImplementation_RegistersBuilderInstanceAsDefaultBlueprint()
    {
        // Arrange
        var builder = BuilderFake.Create();

        // Assert
        Assert.Single(builder.ExposedBlueprints!);
        Assert.Contains(BlueprintBuilder<BuilderFake, TargetFake>.DefaultBlueprintName, builder.ExposedBlueprints!);
        Assert.Same(builder, builder.ExposedBlueprints![builder.DefaultBlueprintNameValue].Invoke());
    }
}
