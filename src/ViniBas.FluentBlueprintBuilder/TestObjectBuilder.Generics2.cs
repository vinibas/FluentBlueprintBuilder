/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

namespace ViniBas.FluentBlueprintBuilder;

/// <summary>
/// Abstract base class to help create test objects using a fluent builder API.
/// It allows defining named blueprints (scenarios) as factory functions, selecting a blueprint
/// by key (or using the first registered blueprint), and overriding blueprint values before
/// constructing the final target object.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type (self-type) used for fluent configuration.
/// This type is also used as the blueprint type (TBlueprint) for the overloaded generic base; blueprints are
/// factory functions that return an instance of <typeparamref name="TBuilder"/> which supplies blueprint values.</typeparam>
/// <typeparam name="TTarget">The type of object produced by the builder's <c>Build</c> method.</typeparam>
/// <remarks>
/// Implementations must provide a public parameterless constructor and register at least one blueprint in <c>ConfigureBlueprints</c>.
/// The base implementation of <c>GetInstance</c> will attempt to construct <typeparamref name="TTarget"/>
/// by matching constructor parameter names to blueprint property names (case-insensitive) and copying blueprint properties 
/// to target properties by name. Override <c>GetInstance</c> to provide custom logic.
/// </remarks>
public abstract class TestObjectBuilder<TBuilder, TTarget>
    : TestObjectBuilder<TBuilder, TBuilder, TTarget>
    where TBuilder : TestObjectBuilder<TBuilder, TTarget>, new()
{
    /// <summary>
    /// Registers a factory that returns the current builder instance in the blueprints dictionary with the key "default".
    /// </summary>
    protected override void ConfigureBlueprints(IDictionary<string, Func<TBuilder>> blueprints)
        => blueprints.Add("default", () => (TBuilder)this);
}