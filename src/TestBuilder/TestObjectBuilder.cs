/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of TestBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Linq.Expressions;
using System.Reflection;

namespace TestBuilder;

public abstract class TestObjectBuilder<T_Builder, T_Presets, T_Target>
    where T_Builder : TestObjectBuilder<T_Builder, T_Presets, T_Target>, new()
{
    private readonly IDictionary<string, Func<T_Presets>> _presets =
        new Dictionary<string, Func<T_Presets>>();

    private T_Presets _selectedPreset = default!;
    private T_Presets SelectedPreset
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
    /// <param name="presetKey">Defines the preset key to use for building the target object. If null, the first preset declared in the list will be used.</param>
    /// <returns></returns>
    public static T_Builder Create(string? presetKey = null)
    {
        var builder = new T_Builder { PresetKey = presetKey };
        builder.ConfigurePresets(builder._presets);
        return builder;
    }

    /// <summary>
    /// Configures the presets available for this builder.
    /// </summary>
    /// <param name="presets">The dictionary to populate with preset key-value pairs.</param>
    protected abstract void ConfigurePresets(IDictionary<string, Func<T_Presets>> presets);

    /// <summary>
    /// Changes the value of a property in the builder.
    /// </summary>
    /// <param name="propertyExpression">Property expression to identify the property to set.</param>
    /// <param name="value">New value to assign to the property.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    public T_Builder Set<T_Value>(Expression<Func<T_Presets, T_Value>> propertyExpression, T_Value value)
    {
        var memberExpression = propertyExpression.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertyExpression));

        var propertyInfo = memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException("Expression must refer to a property.", nameof(propertyExpression));

        propertyInfo.SetValue(SelectedPreset, value);
        return (T_Builder)this;
    }

    /// <summary>
    /// Builds the target object based on the configured properties and selected preset.
    /// </summary>
    /// <returns>The built target object.</returns>
    public virtual T_Target Build() => GetInstance(SelectedPreset);

    /// <summary>
    /// Gets an instance of the target object based on the provided preset.
    /// </summary>
    /// <param name="preset">The preset to use for creating the target object.</param>
    /// <returns>The created target object instance.</returns>
    protected abstract T_Target GetInstance(T_Presets preset);
}