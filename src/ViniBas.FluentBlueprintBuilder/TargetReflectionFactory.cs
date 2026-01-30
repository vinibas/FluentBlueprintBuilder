/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Reflection;

namespace ViniBas.FluentBlueprintBuilder;

internal sealed class TargetReflectionFactory<TTarget>
{
    private Dictionary<string, object?> _blueprintPropsNameValues = [];

    public TTarget InstantiateFromBlueprint<TBlueprint>(TBlueprint blueprint)
    {
        _blueprintPropsNameValues = ListPropertyNamesAndValuesFromTypeAsDictionary(blueprint);

        var selectedCtor = GetTheTargetConstructorWithMoreParametersBeingAllCompatible(out var selectedCtorParams);

        if (selectedCtor is null)
            throw new InvalidOperationException($"No suitable constructor found on {typeof(TTarget).FullName} matching blueprint properties of type {typeof(TBlueprint).FullName}.");

        var instance = CreateTargetInstance(selectedCtor, selectedCtorParams);

        SetTargetValuesToAllCompatibleSetters(instance, selectedCtorParams);

        return instance;
    }

    private static Dictionary<string, object?> ListPropertyNamesAndValuesFromTypeAsDictionary<T>(T instance)
        => typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.GetMethod is not null)
            .ToDictionary(p => p.Name, p => p.GetValue(instance), StringComparer.OrdinalIgnoreCase);

    private ConstructorInfo? GetTheTargetConstructorWithMoreParametersBeingAllCompatible(out ParameterInfo[] selectedCtorParams)
    {
        var targetCtors = ListConstructorsFromType<TTarget>();
        var ctorWithItsParameters = targetCtors.Select<ConstructorInfo, (ConstructorInfo ctor, ParameterInfo[] parameters)>(c => (c, c.GetParameters()));

        foreach (var (ctor, parameters) in ctorWithItsParameters.OrderByDescending(c => c.parameters.Length) )
        {
            selectedCtorParams = parameters;
            if (parameters.All(p => IsParameterCompatible(p)))
                return ctor;
        }

        selectedCtorParams = [];
        return null;
    }

    private static ConstructorInfo[] ListConstructorsFromType<T>()
        => typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private TTarget CreateTargetInstance(ConstructorInfo constructor, ParameterInfo[] selectedCtorParams)
    {
        var ctorArgValues = selectedCtorParams.Select(p => _blueprintPropsNameValues[p.Name ?? string.Empty]).ToArray();
        return (TTarget)constructor.Invoke(ctorArgValues);
    }

    private void SetTargetValuesToAllCompatibleSetters(TTarget instance, ParameterInfo[] parametersAlreadyAssigned)
    {
        var targetSettersOutsideTheCtor = ListOfSettersOfTheTypeThatAreNotInTheParameterExceptionsAsDictionary<TTarget>(parametersAlreadyAssigned);

        foreach (var setter in targetSettersOutsideTheCtor)
        {
            if (!_blueprintPropsNameValues.TryGetValue(setter.Key, out var blueprintPropValue))
                continue;

            if (!IsParameterCompatible(setter.Value.GetParameters()[0], setter.Key))
                continue;

            setter.Value.Invoke(instance, new[] { blueprintPropValue });
        }
    }

    private static Dictionary<string, MethodInfo> ListOfSettersOfTheTypeThatAreNotInTheParameterExceptionsAsDictionary<T>(ParameterInfo[] parameterExceptions)
        => typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.SetMethod is not null)
            .Where(p => !parameterExceptions.Any(sp => sp.Name?.Equals(p.Name, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToDictionary(p => p.Name, p => p.SetMethod!, StringComparer.OrdinalIgnoreCase);

    bool IsParameterCompatible(ParameterInfo parameterInfo, string? parameterName = null)
    {
        if (parameterName is null)
            parameterName = parameterInfo.Name;

        if (!_blueprintPropsNameValues.TryGetValue(parameterName ?? string.Empty, out var blueprintPropValue))
            return false;

        if (blueprintPropValue is null)
            return !parameterInfo.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameterInfo.ParameterType) != null;

        return parameterInfo.ParameterType.IsAssignableFrom(blueprintPropValue?.GetType());
    }
}
