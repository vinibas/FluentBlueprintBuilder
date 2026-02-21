/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Linq.Expressions;
using System.Reflection;

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
    where TBuilder : BlueprintBuilder<TBuilder, TBlueprint, TTarget>, new()
{
    private readonly OrderedDictionary<string, Func<TBlueprint>> _blueprints = new (StringComparer.OrdinalIgnoreCase);

    private readonly ICollection<Action<TBlueprint>> _actionSetters = new List<Action<TBlueprint>>();

    protected string? DefaultBlueprintKey { get; private set; }

    internal ITargetReflectionFactory<TTarget> _targetFactoryInstance = new TargetReflectionFactory<TTarget>();

    /// <summary>
    /// Creates a new instance of the builder object, which can be used to configure and build the target object.
    /// </summary>
    /// <param name="defaultBlueprintKey">Defines the blueprint key to use for building the target object.
    /// If null, the first blueprint declared in the list will be used.
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
    /// <param name="blueprints">The dictionary to populate with blueprint key-value pairs. The keys are case-insensitive.</param>
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

        _actionSetters.Add(selectedBlueprint => propertyInfo.SetValue(selectedBlueprint, convertedValue));

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

    private void ApplySettedValues(TBlueprint selectedBlueprint)
    {
        foreach(var actionSetter in _actionSetters)
            actionSetter(selectedBlueprint);
    }

    /// <summary>
    /// Builds the target object based on the selected blueprint and any configured property overrides.
    /// </summary>
    /// <param name="blueprintKey">
    /// The key of the blueprint to use. If null, with index also null, uses the default key provided
    /// at creation (defaultBlueprintKey). If that is also null, it uses the first registered blueprint.
    /// </param>
    /// <param name="index">
    /// The index of the blueprint to use, based on registration order.
    /// </param>
    /// <returns>The built target object.</returns>
    public virtual TTarget Build(string? blueprintKey = null, uint? index = null)
    {
        var selectedBlueprint = CreateSelectedBlueprint(blueprintKey ?? DefaultBlueprintKey, index);

        ApplySettedValues(selectedBlueprint);

        return GetInstance(selectedBlueprint);
    }

    /// <summary>
    /// Builds a sequence of target objects, one for each specified blueprint key.
    /// Each object is built independently, with all configured <c>Set</c> overrides applied to each one.
    /// This method calls the overridable <see cref="Build(string?, uint?)"/> for each key.
    /// </summary>
    /// <param name="blueprintKeys">A sequence of blueprint keys to build.</param>
    /// <returns>An <see cref="IEnumerable{TTarget}"/> that yields the built objects.</returns>
    public virtual IEnumerable<TTarget> BuildMany(params string[] blueprintKeys)
        => blueprintKeys.Select(k => Build(k));

    /// <summary>
    /// Builds a sequence of a specified number of target objects.
    /// The behavior depends on the <paramref name="blueprintKey"/> parameter.
    /// This method calls the overridable <see cref="Build(string?, uint?)"/> for each item.
    /// </summary>
    /// <param name="size">The number of objects to build.
    /// If null, it defaults to the number of registered blueprints.</param>
    /// <param name="blueprintKey">
    /// If a key is provided, all objects will be built using that specific blueprint.
    /// If null, the method will build objects by iterating through all available blueprints in a circular fashion
    /// (e.g., for a size of 5 with 3 blueprints, it will use blueprint 0, 1, 2, 0, 1).
    /// </param>
    /// <returns>An <see cref="IEnumerable{TTarget}"/> that yields the built objects.</returns>
    public virtual IEnumerable<TTarget> BuildMany(uint? size, string? blueprintKey = null)
    {
        size ??= (uint)_blueprints.Count;

        for (ushort i = 0; i < size; i++)
        {
            uint? blueprintIndex = blueprintKey is not null ?
                null :
                (uint)(i % _blueprints.Count);
            yield return Build(blueprintKey, blueprintIndex);
        }
    }

    private TBlueprint CreateSelectedBlueprint(string? blueprintKey, uint? index = null)
    {
        if (_blueprints.Count == 0)
            throw new InvalidOperationException("No blueprints defined for this builder.");

        if (blueprintKey is not null)
        {
            if (!_blueprints.ContainsKey(blueprintKey))
                throw new KeyNotFoundException($"Blueprint '{blueprintKey}' not found. Available blueprints: {string.Join(", ", _blueprints.Keys)}");

            if (index is not null && _blueprints.GetAt((int)index).Key != blueprintKey)
                throw new ArgumentOutOfRangeException("Index out of range.");

            return _blueprints[blueprintKey].Invoke();
        }

        if (index is not null)
            return _blueprints.GetAt((int)index).Value.Invoke();

        return DefaultBlueprintKey is not null ?
            _blueprints[DefaultBlueprintKey].Invoke() :
            _blueprints.First().Value.Invoke();
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
}
