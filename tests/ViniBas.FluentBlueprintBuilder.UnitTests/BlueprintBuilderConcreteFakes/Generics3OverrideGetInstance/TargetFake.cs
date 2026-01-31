/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Text;

namespace ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics3OverrideGetInstance;

public class TargetFake
{
    public string Name { get; set; } = string.Empty;
    public StringBuilder Metadata { get; set; } = new("InitialMetadata");
}
