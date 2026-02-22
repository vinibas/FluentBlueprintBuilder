/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Text;

namespace ViniBas.FluentBlueprintBuilder.UnitTests.BlueprintBuilderConcreteFakes.Generics3;

public record BlueprintFake(string BlueprintKey, string Name, StringBuilder Metadata, int Counter);

public record BlueprintFakeMissingName(StringBuilder Metadata);

public record BlueprintFakeMissingMetadata(string Name);
