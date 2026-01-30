/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

namespace ViniBas.FluentBlueprintBuilder.UnitTests.TargetReflectionFactoryTestsConcreteFakes;

public sealed class MixedTarget
{
    public string PropA { get; }
    public int PropB { get; private set; }
    public bool PropC { get; }
    public DateTime PropD = DateTime.MinValue;
    public byte PropE { get; set; } = 2;
    public decimal PropF { get; set; }

    #nullable disable
    public MixedTarget() {}
    #nullable restore

    private MixedTarget(string propA)
        => PropA = propA + "_suitable_constructor";

    #nullable disable
    private MixedTarget(string propA, string argExtra) { }
    #nullable restore
}
