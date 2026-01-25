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
public abstract class TestObjectBuilder<TBuilder, TBlueprint, TTarget>
    where TBuilder : TestObjectBuilder<TBuilder, TBlueprint, TTarget>, new()
{
    private readonly IDictionary<string, Func<TBlueprint>> _blueprints =
        new Dictionary<string, Func<TBlueprint>>(StringComparer.OrdinalIgnoreCase);

    private TBlueprint _selectedBlueprint = default!;
    private TBlueprint SelectedBlueprint
    {
        get
        {
            if (_selectedBlueprint is not null)
                return _selectedBlueprint;

            if (_blueprints.Count == 0)
                throw new InvalidOperationException("No variants defined for this builder.");
            
            if (BlueprintKey is not null && !_blueprints.ContainsKey(BlueprintKey))
                throw new KeyNotFoundException($"Blueprint '{BlueprintKey}' not found. Available blueprints: {string.Join(", ", _blueprints.Keys)}");
        
            _selectedBlueprint = BlueprintKey is not null ?
                _blueprints[BlueprintKey].Invoke() :
                _blueprints.First().Value.Invoke();
            
            return _selectedBlueprint;
        }
    }

    protected string? BlueprintKey { get; private set; }
    
    /// <summary>
    /// Creates a new instance of the builder object, which can be used to configure and build the target object.
    /// </summary>
    /// <param name="blueprintKey">Defines the blueprint key to use for building the target object.
    /// If null, the first blueprint declared in the list will be used.
    /// blueprintKey is case-insensitive.</param>
    /// <returns></returns>
    public static TBuilder Create(string? blueprintKey = null)
    {
        var builder = new TBuilder { BlueprintKey = blueprintKey };
        builder.ConfigureBlueprints(builder._blueprints);
        return builder;
    }

    /// <summary>
    /// Configures the blueprints available for this builder.
    /// </summary>
    /// <param name="blueprints">The dictionary to populate with blueprint key-value pairs. The keys are case-insensitive.</param>
    protected abstract void ConfigureBlueprints(IDictionary<string, Func<TBlueprint>> blueprints);

    /// <summary>
    /// Changes the value of a property in the builder.
    /// </summary>
    /// <param name="propertyExpression">Property expression to identify the property to set.</param>
    /// <param name="value">New value to assign to the property.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    public TBuilder Set<TValue>(Expression<Func<TBlueprint, TValue>> propertyExpression, TValue value)
    {
        var memberExpression = propertyExpression.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertyExpression));

        var propertyInfo = memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException("Expression must refer to a property.", nameof(propertyExpression));

        propertyInfo.SetValue(SelectedBlueprint, value);
        return (TBuilder)this;
    }

    /// <summary>
    /// Builds the target object based on the configured properties and selected blueprint.
    /// </summary>
    /// <returns>The built target object.</returns>
    public virtual TTarget Build() => GetInstance(SelectedBlueprint);

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
    {
        if (blueprint is null)
            return default!;

        var blueprintType = typeof(TBlueprint);
        var targetType = typeof(TTarget);

        var blueprintPropsNameValues = ListPropertyNamesAndValuesFromTypeAsDictionary(blueprintType);
        var targetCtors = ListConstructorsFromType(targetType);

        var selectedCtor = GetConstructorWithMoreParametersBeingAllCompatible(targetCtors, out var selectedCtorParams);

        if (selectedCtor is null)
            throw new InvalidOperationException($"No suitable constructor found on {targetType.FullName} matching blueprint properties of type {blueprintType.FullName}.");
        
        var ctorArgValues = selectedCtorParams.Select(p => blueprintPropsNameValues[p.Name ?? string.Empty]).ToArray();
        
        var instance = (TTarget)selectedCtor.Invoke(ctorArgValues);
        
        var targetSettersOutsideTheCtor = ListOfSettersOfTheTypeThatAreNotInTheParameterExceptionsAsDictionary(targetType, selectedCtorParams);

        SetValuesToAllCompatibleSetters(targetSettersOutsideTheCtor, blueprintPropsNameValues);

        return instance;


        Dictionary<string, object?> ListPropertyNamesAndValuesFromTypeAsDictionary(Type type)
            => type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetMethod is not null)
                .ToDictionary(p => p.Name, p => p.GetValue(blueprint), StringComparer.OrdinalIgnoreCase);

        ConstructorInfo[] ListConstructorsFromType(Type type)
            => targetType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        ConstructorInfo? GetConstructorWithMoreParametersBeingAllCompatible(IEnumerable<ConstructorInfo> constructors, out ParameterInfo[] selectedCtorParams)
        {
            var ctorWithItsParameters = targetCtors.Select<ConstructorInfo, (ConstructorInfo ctor, ParameterInfo[] parameters)>(c => (c, c.GetParameters()));

            foreach (var ctorWithParam in ctorWithItsParameters.OrderByDescending(c => c.parameters.Length) )
            {
                selectedCtorParams = ctorWithParam.parameters;
                if (selectedCtorParams.All(p => IsParameterCompatible(p)))
                    return ctorWithParam.ctor;
            }

            selectedCtorParams = [];
            return null;
        }
        
        Dictionary<string, MethodInfo> ListOfSettersOfTheTypeThatAreNotInTheParameterExceptionsAsDictionary(Type type, ParameterInfo[] parameterExceptions)
            => targetType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.SetMethod is not null)
                .Where(p => !parameterExceptions.Any(sp => sp.Name?.Equals(p.Name, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToDictionary(p => p.Name, p => p.SetMethod!, StringComparer.OrdinalIgnoreCase);

        void SetValuesToAllCompatibleSetters(Dictionary<string, MethodInfo> settersToSet, Dictionary<string, object?> valuesToSet)
        {
            foreach (var setter in settersToSet)
            {
                if (!valuesToSet.TryGetValue(setter.Key, out var blueprintPropValue))
                    continue;
                
                if (!IsParameterCompatible(setter.Value.GetParameters()[0], setter.Key))
                    continue;

                setter.Value.Invoke(instance, new[] { blueprintPropValue });
            }
        }

        bool IsParameterCompatible(ParameterInfo parameterInfo, string? parameterName = null)
        {
            if (parameterName is null)
                parameterName = parameterInfo.Name;
            
            if (!blueprintPropsNameValues.TryGetValue(parameterName ?? string.Empty, out var blueprintPropValue))
                return false;
            
            if (blueprintPropValue is null)
                return !parameterInfo.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameterInfo.ParameterType) != null;
            
            return parameterInfo.ParameterType.IsAssignableFrom(blueprintPropValue?.GetType());
        }
    }
}