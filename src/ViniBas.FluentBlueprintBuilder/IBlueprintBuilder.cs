/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

namespace ViniBas.FluentBlueprintBuilder;

/// <summary>
/// Represents a builder that can produce instances of <typeparamref name="TTarget"/>.
/// This interface enables dependency injection and mocking of builders in unit tests.
/// </summary>
/// <typeparam name="TTarget">The type of object produced by the builder.</typeparam>
public interface IBlueprintBuilder<TTarget>
{
    /// <inheritdoc cref="BlueprintBuilder{TBuilder, TBlueprint, TTarget}.Build(string?, uint?)"/>
    TTarget Build(string? blueprintKey = null, uint? index = null);

    /// <inheritdoc cref="BlueprintBuilder{TBuilder, TBlueprint, TTarget}.BuildMany(string[])"/>
    IEnumerable<TTarget> BuildMany(params string[] blueprintKeys);

    /// <inheritdoc cref="BlueprintBuilder{TBuilder, TBlueprint, TTarget}.BuildMany(uint?, string[])"/>
    IEnumerable<TTarget> BuildMany(uint? size, params string[] blueprintKeys);
}
