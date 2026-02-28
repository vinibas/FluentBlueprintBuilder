/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ViniBas.FluentBlueprintBuilder;

/// <summary>
/// Abstract base class to help create test objects using a fluent builder API.
/// It allows defining named blueprints (scenarios) as factory functions, selecting a blueprint
/// by key (or using the first registered blueprint), and overriding blueprint values before
/// constructing the final target object.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type (self-type) used for fluent configuration.</typeparam>
/// <typeparam name="TBlueprint">Type that holds the blueprint values used to build the target.</typeparam>
/// <typeparam name="TTarget">The type of object produced by the builder's <c>Build</c> method.</typeparam>
/// <remarks>
/// Implementations must provide a public parameterless constructor and register at least one blueprint in <c>ConfigureBlueprints</c>.
/// The base implementation of <c>GetInstance</c> will attempt to construct <typeparamref name="TTarget"/>
/// by matching constructor parameter names to blueprint property names (case-insensitive) and copying blueprint properties
/// to target properties by name. Override <c>GetInstance</c> to provide custom logic.
/// </remarks>
public abstract class BlueprintBuilder<TBuilder, TBlueprint, TTarget>
    : IBlueprintBuilder<TTarget>
    where TBuilder : BlueprintBuilder<TBuilder, TBlueprint, TTarget>, new()
{
    private readonly OrderedDictionary<string, Func<TBlueprint>> _blueprints = new (StringComparer.OrdinalIgnoreCase);

    private readonly ICollection<Action<TBlueprint>> _actionSetters = new List<Action<TBlueprint>>();

    protected string? DefaultBlueprintKey { get; private set; }

    /// <summary>
    /// Gets the keys of all registered blueprints, in registration order.
    /// Useful for verifying blueprint configuration in unit tests.
    /// </summary>
    protected IReadOnlyList<string> RegisteredBlueprintKeys
        => _blueprints.Keys.ToList().AsReadOnly();

    internal ITargetReflectionFactory<TTarget> _targetFactoryInstance = new TargetReflectionFactory<TTarget>();

    /// <summary>
    /// Creates a new instance of the builder object, which can be used to configure and build the target object.
    /// </summary>
    /// <param name="defaultBlueprintKey">Sets the per-instance default blueprint key, used as a fallback by <see cref="Build(string?, uint?)"/>
    /// when no key or index is explicitly provided. If null, <c>Build</c> will fall back to the first registered blueprint.
    /// The key is case-insensitive.</param>
    /// <returns>A new builder instance.</returns>
    public static TBuilder Create(string? defaultBlueprintKey = null)
        => new TBuilder { DefaultBlueprintKey = defaultBlueprintKey };

    protected BlueprintBuilder()
        => ConfigureBuilder((TBuilder)this);

    /// <summary>
    /// Configures the builder instance by registering blueprints and applying default values.
    /// </summary>
    /// <param name="builder">The builder instance to configure.</param>
    private static void ConfigureBuilder(TBuilder builder)
    {
        builder.ConfigureBlueprints(builder._blueprints);
        builder.ConfigureDefaultValues();
    }

    /// <summary>
    /// Configures the default values for the blueprint properties.
    /// <para>
    /// This method is called automatically during the builder initialization.
    /// Override this method to use <see cref="Set{TValue}(Expression{Func{TBlueprint, TValue}}, TValue)"/>
    /// (or its overloads) to establish a baseline state for your data (e.g., generating random IDs, setting default flags).
    /// </para>
    /// <para>
    /// These values are applied after the blueprint is instantiated but can be overridden by subsequent
    /// <c>Set</c> calls in the fluent chain.
    /// </para>
    /// </summary>
    protected virtual void ConfigureDefaultValues() { }

    /// <summary>
    /// Configures the blueprints available for this builder.
    /// </summary>
    /// <param name="blueprints">The dictionary to populate with named blueprint factory functions. Keys are case-insensitive.
    /// Each value is a <c>Func&lt;TBlueprint&gt;</c> that is invoked each time a blueprint is selected,
    /// producing a fresh <typeparamref name="TBlueprint"/> instance whose property values will be used
    /// to construct the target <typeparamref name="TTarget"/> object.</param>
    protected abstract void ConfigureBlueprints(IDictionary<string, Func<TBlueprint>> blueprints);

    /// <summary>
    /// Overrides the value of a property in the blueprint with a static value.
    /// This override is applied before the target is built.
    /// </summary>
    /// <typeparam name="TValue">The type of the property.</typeparam>
    /// <param name="propertyExpression">A lambda expression identifying the property to set (e.g., <c>x => x.PropertyName</c>).</param>
    /// <param name="value">The new value to assign to the property.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TBuilder Set<TValue>(Expression<Func<TBlueprint, TValue>> propertyExpression, TValue value)
    {
        var propertyInfo = GetPropertyInfoFromExpression(propertyExpression);
        var convertedValue = ConvertValue(value, propertyInfo.PropertyType);

        _actionSetters.Add(selectedBlueprint
            => propertyInfo.SetValue(selectedBlueprint, convertedValue));

        return (TBuilder)this;
    }

    /// <summary>
    /// Overrides the value of a property in the blueprint using a value generator function.
    /// The function receives the current blueprint instance, allowing values to depend on other properties.
    /// This override is applied before the target is built.
    /// </summary>
    /// <typeparam name="TValue">The type of the property.</typeparam>
    /// <param name="propertyExpression">A lambda expression identifying the property to set (e.g., <c>x => x.PropertyName</c>).</param>
    /// <param name="valueFactory">A function that generates the value. It receives the blueprint instance as a parameter.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TBuilder Set<TValue>(Expression<Func<TBlueprint, TValue>> propertyExpression, Func<TBlueprint, TValue> valueFactory)
    {
        var propertyInfo = GetPropertyInfoFromExpression(propertyExpression);
        _actionSetters.Add(selectedBlueprint =>
        {
            var value = valueFactory(selectedBlueprint);
            var convertedValue = ConvertValue(value, propertyInfo.PropertyType);
            propertyInfo.SetValue(selectedBlueprint, convertedValue);
        });
        return (TBuilder)this;
    }

    private static PropertyInfo GetPropertyInfoFromExpression<TValue>(Expression<Func<TBlueprint, TValue>> propertyExpression)
    {
        var expression = propertyExpression.Body;

        // Handle implicit conversions (e.g. int passed to byte property) which create a UnaryExpression (Convert)
        if (expression is UnaryExpression unaryExpression && (unaryExpression.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked))
            expression = unaryExpression.Operand;

        var memberExpression = expression as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertyExpression));

        var propertyInfo = memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException("Expression must refer to a property.", nameof(propertyExpression));

        return propertyInfo;
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null) return null;
        if (targetType.IsInstanceOfType(value)) return value;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return Convert.ChangeType(value, underlyingType);
    }

    /// <summary>
    /// Builds the target object based on the selected blueprint and any configured property overrides.
    /// </summary>
    /// <param name="blueprintKey">
    /// The key of the blueprint to use (case-insensitive).
    /// If null and <paramref name="index"/> is also null, falls back to the default key set at creation;
    /// if that is also null, the first registered blueprint is used.
    /// </param>
    /// <param name="index">
    /// Zero-based index of the blueprint to use, based on registration order.
    /// </param>
    /// <returns>The built target object.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="blueprintKey"/> is provided but not found.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when both <paramref name="blueprintKey"/> and <paramref name="index"/> are provided but do not refer to the same blueprint.</exception>
    public virtual TTarget Build(string? blueprintKey = null, uint? index = null)
    {
        var selectedBlueprint = CreateSelectedBlueprint(blueprintKey, index);

        ApplySettedValues(selectedBlueprint);

        return GetInstance(selectedBlueprint);
    }

    private void ApplySettedValues(TBlueprint selectedBlueprint)
    {
        foreach(var actionSetter in _actionSetters)
            actionSetter(selectedBlueprint);
    }

    /// <summary>
    /// Builds a sequence of target objects, one for each specified blueprint key.
    /// Each object is built independently, with all configured <c>Set</c> overrides applied to each one.
    /// Equivalent to calling <see cref="BuildMany(uint?, string[])"/> with <c>size</c> omitted.
    /// Returns an empty sequence if no keys are provided.
    /// </summary>
    /// <param name="blueprintKeys">A sequence of blueprint keys to build.</param>
    /// <returns>An <see cref="IEnumerable{TTarget}"/> that yields the built objects, in the same order as the keys.</returns>
    public virtual IEnumerable<TTarget> BuildMany(params string[] blueprintKeys)
        => blueprintKeys.Length > 0 ? BuildMany(null, blueprintKeys) : Enumerable.Empty<TTarget>();

    /// <summary>
    /// Builds a sequence of a specified number of target objects, cycling through the provided blueprint keys.
    /// This method calls the overridable <see cref="Build(string?, uint?)"/> for each item.
    /// </summary>
    /// <param name="size">The number of objects to build.
    /// If null, defaults to the number of provided keys (if any), or to the total number of registered blueprints otherwise.</param>
    /// <param name="blueprintKeys">
    /// The blueprint keys to cycle through when building the objects.
    /// If provided, objects are built by cycling through the given keys in order
    /// (e.g., for a size of 5 with keys "a" and "b", it will use a, b, a, b, a).
    /// If empty, all registered blueprints are cycled through in registration order
    /// (e.g., for a size of 5 with 3 blueprints, it will use blueprint 0, 1, 2, 0, 1).
    /// </param>
    /// <returns>An <see cref="IEnumerable{TTarget}"/> that yields the built objects.</returns>
    #if NET9_0_OR_GREATER
    [OverloadResolutionPriority(1)]
    #endif
    public virtual IEnumerable<TTarget> BuildMany(uint? size, params string[] blueprintKeys)
    {
        size ??= (uint) (blueprintKeys.Length > 0 ? blueprintKeys.Length : _blueprints.Count);

        for (var i = 0; i < size; i++)
        {
            var currentBlueprintKey = blueprintKeys.Length > 0 ?
                blueprintKeys[i % blueprintKeys.Length] :
                null;

            uint? currentBlueprintIndex = currentBlueprintKey is not null ?
                null :
                (uint)(i % _blueprints.Count);

            yield return Build(currentBlueprintKey, currentBlueprintIndex);
        }
    }

    /// <summary>
    /// Creates a blueprint instance for the specified key, without applying any <c>Set</c> overrides.
    /// This is useful for unit testing the blueprint configuration in isolation from the target construction.
    /// </summary>
    /// <param name="blueprintKey">The key of the blueprint to create (case-insensitive).</param>
    /// <returns>A fresh blueprint instance as produced by the registered factory function.</returns>
    protected TBlueprint CreateBlueprint(string blueprintKey)
        => CreateSelectedBlueprint(blueprintKey);

    private TBlueprint CreateSelectedBlueprint(string? blueprintKey, uint? index = null)
    {
        if (_blueprints.Count == 0)
            throw new InvalidOperationException("No blueprints defined for this builder.");

        if (blueprintKey is not null)
        {
            ValidateBlueprintKeyExistence(blueprintKey);

            if (index is not null && _blueprints.IndexOf(blueprintKey) != (int)index)
                throw new ArgumentOutOfRangeException(nameof(index), "The provided index does not match the provided blueprint key.");

            return _blueprints[blueprintKey].Invoke();
        }

        if (index is not null)
        {
            if (_blueprints.Count <= index)
                throw new ArgumentOutOfRangeException(nameof(index), "The provided index is out of range.");

            return _blueprints.GetAt((int)index).Value.Invoke();
        }

        if (DefaultBlueprintKey is not null)
        {
            ValidateBlueprintKeyExistence(DefaultBlueprintKey);
            return _blueprints[DefaultBlueprintKey].Invoke();
        }

        return _blueprints.GetAt(0).Value.Invoke();


        void ValidateBlueprintKeyExistence(string blueprintKey)
        {
            if (!_blueprints.ContainsKey(blueprintKey))
                throw new KeyNotFoundException($"Blueprint '{blueprintKey}' not found. Available blueprints: {string.Join(", ", _blueprints.Keys)}");
        }
    }

    /// <summary>
    /// Gets an instance of the target object based on the provided blueprint.
    /// The default implementation:
    /// 1. Collects blueprint properties (case-insensitive).
    /// 2. Finds a constructor on TTarget whose parameter names (case-insensitive) are all present
    ///    in the blueprint properties. Constructors are considered in descending order of parameter count
    ///    (preference to constructors with more parameters). If multiple constructors satisfy this,
    ///    the one with more parameters is chosen. If none is found, an <see cref="InvalidOperationException"/>
    ///    is thrown.
    /// 3. Invokes the selected constructor using blueprint values matched by parameter name.
    /// 4. After construction, assigns any remaining blueprint properties (those not passed to the constructor)
    ///    to writable target properties with the same name (case-insensitive). Extra blueprint properties
    ///    or target properties without matching blueprint are ignored.
    /// Notes:
    /// - Both public and non-public constructors/properties are considered.
    /// - This default implementation requires blueprint property values to be type-compatible with
    /// constructor/setter parameter types; it does not perform implicit conversions.
    /// </summary>
    /// <param name="blueprint">The blueprint instance to use for creating the target object.</param>
    /// <returns>The created target object instance.</returns>
    protected virtual TTarget GetInstance(TBlueprint blueprint)
        => blueprint is null ?
            default! :
            _targetFactoryInstance.InstantiateFromBlueprint(blueprint);

    public virtual TBuilder Clone()
    {
        var clone = new TBuilder
        {
            DefaultBlueprintKey = DefaultBlueprintKey,
            _targetFactoryInstance = _targetFactoryInstance,
        };

        clone._actionSetters.Clear();

        foreach (var setter in _actionSetters)
            clone._actionSetters.Add(setter);

        return clone;
    }
}
