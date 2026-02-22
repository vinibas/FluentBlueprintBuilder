/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Text;

namespace ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics3;

public class TargetFake
{
    public string BlueprintKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public StringBuilder Metadata { get; set; } = new("InitialMetadata");
    public int Counter { get; set; }
}

public class TargetFakeWithRequiredConstructor
{
    // Field public to verify assignment via constructor, without setter to avoid property assignment
    public string Name;
    public StringBuilder Metadata { get; set; } = new("InitialMetadata");
    public DateTime SomeDate { get; set; } = new DateTime(2026, 2, 10);

    public TargetFakeWithRequiredConstructor(string name) => Name = name;
}
