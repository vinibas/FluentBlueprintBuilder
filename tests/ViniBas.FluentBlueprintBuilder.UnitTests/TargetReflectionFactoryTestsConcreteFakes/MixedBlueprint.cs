/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

namespace ViniBas.FluentBlueprintBuilder.UnitTests.TargetReflectionFactoryTestsConcreteFakes;

public sealed class MixedBlueprint
{
    public string PropA { get; set; } = string.Empty;
    public int PropB { get; set; }
    public bool PropC { get; set; }
    public DateTime PropD { get; set; }
    public byte PropE { private get; set; }
    public decimal PropF;
}
