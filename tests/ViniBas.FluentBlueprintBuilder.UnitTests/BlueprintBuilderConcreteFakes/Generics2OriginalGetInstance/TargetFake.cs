/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Text;

namespace ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics2OriginalGetInstance;

public sealed class TargetFake
{
    // Field public to verify assignment via constructor, without setter to avoid property assignment
    public string Name;
    public StringBuilder Metadata { get; set; } = new("InitialMetadata");
    public DateTime SomeDate { get; set; } = new DateTime(2026, 2, 10);

    public TargetFake(string name)
        => Name = name;
}
