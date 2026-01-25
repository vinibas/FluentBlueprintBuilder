/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of TestBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Linq.Expressions;
using System.Reflection;

namespace TestBuilder;

/// <summary>
/// Abstract base class to help create test objects using a fluent builder API.
/// It allows defining named presets (scenarios) as factory functions, selecting a preset
/// by key (or using the first registered preset), and overriding preset values before
/// constructing the final target object.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type (self-type) used for fluent configuration.</typeparam>
/// <typeparam name="TPresets">Type that holds the preset values used to build the target.</typeparam>
/// <typeparam name="TTarget">The type of object produced by the builder's <c>Build</c> method.</typeparam>
/// <remarks>
/// Implementations must provide a public parameterless constructor and register at least one preset in <c>ConfigurePresets</c>.
/// The base implementation of <c>GetInstance</c> will attempt to construct <typeparamref name="TTarget"/>
/// by matching constructor parameter names to preset property names (case-insensitive) and copying preset properties 
/// to target properties by name. Override <c>GetInstance</c> to provide custom logic.
/// </remarks>
public abstract class TestObjectBuilder<TBuilder, TPresets, TTarget>
    where TBuilder : TestObjectBuilder<TBuilder, TPresets, TTarget>, new()
{
    private readonly IDictionary<string, Func<TPresets>> _presets =
        new Dictionary<string, Func<TPresets>>(StringComparer.OrdinalIgnoreCase);

    private TPresets _selectedPreset = default!;
    private TPresets SelectedPreset
    {
        get
        {
            if (_selectedPreset is not null)
                return _selectedPreset;

            if (_presets.Count == 0)
                throw new InvalidOperationException("No variants defined for this builder.");
            
            if (PresetKey is not null && !_presets.ContainsKey(PresetKey))
                throw new KeyNotFoundException($"Preset '{PresetKey}' not found. Available presets: {string.Join(", ", _presets.Keys)}");
        
            _selectedPreset = PresetKey is not null ?
                _presets[PresetKey].Invoke() :
                _presets.First().Value.Invoke();
            
            return _selectedPreset;
        }
    }

    protected string? PresetKey { get; private set; }
    
    /// <summary>
    /// Creates a new instance of the builder object, which can be used to configure and build the target object.
    /// </summary>
    /// <param name="presetKey">Defines the preset key to use for building the target object.
    /// If null, the first preset declared in the list will be used.
    /// presetKey is case-insensitive.</param>
    /// <returns></returns>
    public static TBuilder Create(string? presetKey = null)
    {
        var builder = new TBuilder { PresetKey = presetKey };
        builder.ConfigurePresets(builder._presets);
        return builder;
    }

    /// <summary>
    /// Configures the presets available for this builder.
    /// </summary>
    /// <param name="presets">The dictionary to populate with preset key-value pairs. The keys are case-insensitive.</param>
    protected abstract void ConfigurePresets(IDictionary<string, Func<TPresets>> presets);

    /// <summary>
    /// Changes the value of a property in the builder.
    /// </summary>
    /// <param name="propertyExpression">Property expression to identify the property to set.</param>
    /// <param name="value">New value to assign to the property.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    public TBuilder Set<TValue>(Expression<Func<TPresets, TValue>> propertyExpression, TValue value)
    {
        var memberExpression = propertyExpression.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertyExpression));

        var propertyInfo = memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException("Expression must refer to a property.", nameof(propertyExpression));

        propertyInfo.SetValue(SelectedPreset, value);
        return (TBuilder)this;
    }

    /// <summary>
    /// Builds the target object based on the configured properties and selected preset.
    /// </summary>
    /// <returns>The built target object.</returns>
    public virtual TTarget Build() => GetInstance(SelectedPreset);

    /// <summary>
    /// Gets an instance of the target object based on the provided preset.
    /// The default implementation:
    /// 1. Collects preset properties (case-insensitive).
    /// 2. Finds a constructor on TTarget whose parameter names (case-insensitive) are all present
    ///    in the preset properties. Constructors are considered in descending order of parameter count
    ///    (preference to constructors with more parameters). If multiple constructors satisfy this,
    ///    the one with more parameters is chosen. If none is found, an <see cref="InvalidOperationException"/>
    ///    is thrown.
    /// 3. Invokes the selected constructor using preset values matched by parameter name.
    /// 4. After construction, assigns any remaining preset properties (those not passed to the constructor)
    ///    to writable target properties with the same name (case-insensitive). Extra preset properties
    ///    or target properties without matching preset are ignored.
    /// Notes:
    /// - Both public and non-public constructors/properties are considered.
    /// - This default implementation requires preset property values to be type-compatible with 
    /// constructor/setter parameter types; it does not perform implicit conversions.
    /// </summary>
    /// <param name="preset">The preset instance to use for creating the target object.</param>
    /// <returns>The created target object instance.</returns>
    protected virtual TTarget GetInstance(TPresets preset)
    {
        if (preset is null)
            return default!;

        var presetType = typeof(TPresets);
        var targetType = typeof(TTarget);

        var presetPropsNameValues = ListPropertyNamesAndValuesFromTypeAsDictionary(presetType);
        var targetCtors = ListConstructorsFromType(targetType);

        var selectedCtor = GetConstructorWithMoreParametersBeingAllCompatible(targetCtors, out var selectedCtorParams);

        if (selectedCtor is null)
            throw new InvalidOperationException($"No suitable constructor found on {targetType.FullName} matching preset properties of type {presetType.FullName}.");
        
        var ctorArgValues = selectedCtorParams.Select(p => presetPropsNameValues[p.Name ?? string.Empty]).ToArray();
        
        var instance = (TTarget)selectedCtor.Invoke(ctorArgValues);
        
        var targetSettersOutsideTheCtor = ListOfSettersOfTheTypeThatAreNotInTheParameterExceptionsAsDictionary(targetType, selectedCtorParams);

        SetValuesToAllCompatibleSetters(targetSettersOutsideTheCtor, presetPropsNameValues);

        return instance;


        Dictionary<string, object?> ListPropertyNamesAndValuesFromTypeAsDictionary(Type presetType)
            => presetType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetMethod is not null)
                .ToDictionary(p => p.Name, p => p.GetValue(preset), StringComparer.OrdinalIgnoreCase);

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
                if (!valuesToSet.TryGetValue(setter.Key, out var presetPropValue))
                    continue;
                
                if (!IsParameterCompatible(setter.Value.GetParameters()[0], setter.Key))
                    continue;

                setter.Value.Invoke(instance, new[] { presetPropValue });
            }
        }

        bool IsParameterCompatible(ParameterInfo parameterInfo, string? parameterName = null)
        {
            if (parameterName is null)
                parameterName = parameterInfo.Name;
            
            if (!presetPropsNameValues.TryGetValue(parameterName ?? string.Empty, out var presetPropValue))
                return false;
            
            if (presetPropValue is null)
                return !parameterInfo.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameterInfo.ParameterType) != null;
            
            return parameterInfo.ParameterType.IsAssignableFrom(presetPropValue?.GetType());
        }
    }
}