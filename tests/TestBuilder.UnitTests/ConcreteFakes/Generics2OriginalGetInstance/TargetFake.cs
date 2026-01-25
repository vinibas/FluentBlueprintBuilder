/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of TestBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

namespace TestBuilder.UnitTests.ConcreteFakes.Generics2OriginalGetInstance;

public class TargetFake
{
    // Field public to verify assignment via constructor, without setter to avoid property assignment
    public List<string> Tags = [ "InitialTag1", "InitialTag2" ];
    public Dictionary<string, object> Metadata { get; set; } =
        new() { ["Initialkey"] = "Initial value" };
    public DateTime SomeDate { get; set; } = new DateTime(2026, 2, 10);

    public TargetFake(List<string> tags)
        => Tags = tags;
}
