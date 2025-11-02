// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using System.Linq.Expressions;
using System.Reflection;
using Moq;
using Moq.Language.Flow;

namespace MoqProxy.Internals;

/// <summary>
/// Provides methods for setting up property proxying on mock instances.
/// </summary>
internal static class PropertySetup
{
    /// <summary>
    /// Cached reflection reference to the generic <see cref="SetupRegularProperty{T, TProp}"/> method.
    /// Used to invoke the method with runtime type arguments for property setup.
    /// </summary>
    private static readonly MethodInfo OpenGenericSetupPropMethod =
        typeof(PropertySetup)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .SingleOrDefault(m =>
            {
                if (m is not { Name: nameof(SetupRegularProperty), IsGenericMethodDefinition: true })
                {
                    return false;
                }

                var genericArgs = m.GetGenericArguments();
                if (genericArgs.Length != 2)
                {
                    return false;
                }

                var parameters = m.GetParameters();
                if (parameters.Length != 3)
                {
                    return false;
                }

                // Check parameter types: Mock<T>, T, PropertyInfo
                var param0 = parameters[0].ParameterType;
                var param1 = parameters[1].ParameterType;
                var param2 = parameters[2].ParameterType;

                return param0.IsGenericType
                       && param0.GetGenericTypeDefinition() == typeof(Mock<>)
                       && param0.GetGenericArguments()[0] == genericArgs[0] // Mock<T> where T is first generic param
                       && param1 == genericArgs[0] // T (first generic param)
                       && param2 == typeof(PropertyInfo);
            })
        ?? throw new InvalidOperationException(
            $"""
             Failed to find method '{nameof(SetupRegularProperty)}<T, TProp>' on type '{typeof(PropertySetup).FullName}'.
             Expected: a generic method with 2 type parameters (T, TProp) and 3 parameters (Mock<T>, T, PropertyInfo).
             This is an internal reflection error in the mock proxy setup.
             """);

    /// <summary>
    /// Cached reflection reference to the generic <see cref="It.IsAny{TValue}"/> method from Moq.
    /// Used to create It.IsAny expressions for method and indexer parameter matching.
    /// </summary>
    private static readonly MethodInfo OpenGenericMockItIsAnyMethod =
        typeof(It).GetMethod(nameof(It.IsAny))
        ?? throw new InvalidOperationException(
            $"""
             Failed to find method '{nameof(It.IsAny)}' on type '{typeof(It).FullName}'. 
             This may indicate an incompatible version of Moq library.
             """);

    /// <summary>
    /// Sets up all public instance properties on the mock to forward to the implementation.
    /// Distinguishes between regular properties and indexers, delegating to appropriate setup methods.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward property access to.</param>
    internal static void SetupProperties<T>(
        Mock<T> mock,
        T impl)
        where T : class
    {
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var indexParams = prop.GetIndexParameters();

            if (indexParams.Length > 0)
            {
                SetupIndexerProperty(mock, impl, prop, indexParams);
            }
            else
            {
                SetupRegularProperty(mock, impl, prop);
            }
        }
    }

    /// <summary>
    /// Sets up an indexer property on the mock to forward to the implementation.
    /// Uses reflection to call the appropriate generic setup method based on the number of index parameters.
    /// Skips indexers with by-ref or ref-like parameters which are not supported.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward indexer access to.</param>
    /// <param name="prop">The property info representing the indexer.</param>
    /// <param name="indexParams">The index parameters of the indexer.</param>
    private static void SetupIndexerProperty<T>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop,
        ParameterInfo[] indexParams)
        where T : class
    {
        // Skip indexers with by-ref or ref-like parameters
        if (indexParams.Any(p => p.ParameterType.IsByRef || p.ParameterType.IsByRefLike))
        {
            return;
        }

        var indexParamTypes = indexParams.Select(p => p.ParameterType).ToArray();
        var allTypes = indexParamTypes.Concat([prop.PropertyType]).ToArray();

        // Use reflection to call the generic helper - need to find the right overload based on parameter count
        var helperMethod = typeof(PropertySetup)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m is { Name: nameof(SetupIndexerTyped), IsGenericMethodDefinition: true })
            .FirstOrDefault(m => m.GetGenericArguments().Length == allTypes.Length + 1); // +1 for T

        if (helperMethod == null)
        {
            // Unsupported indexer arity (more than 2 parameters)
            return;
        }

        helperMethod = helperMethod.MakeGenericMethod([typeof(T), .. allTypes]);
        helperMethod.Invoke(null, [mock, impl, prop]);
    }

    /// <summary>
    /// Sets up an indexer with zero parameters (edge case).
    /// Delegates to getter and setter setup methods.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TProp">The property type of the indexer.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance.</param>
    /// <param name="prop">The property info representing the indexer.</param>
    private static void SetupIndexerTyped<T, TProp>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop)
        where T : class
    {
        // 0-parameter indexer (shouldn't happen, but handle it)
        SetupIndexerGetter<T, TProp>(mock, impl, prop, []);
        SetupIndexerSetter<T, TProp>(mock, impl, prop, []);
    }

    /// <summary>
    /// Sets up an indexer with one index parameter.
    /// Delegates to getter and setter setup methods with the appropriate type information.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TIndex">The type of the index parameter.</typeparam>
    /// <typeparam name="TProp">The property type of the indexer.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance.</param>
    /// <param name="prop">The property info representing the indexer.</param>
    private static void SetupIndexerTyped<T, TIndex, TProp>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop)
        where T : class
    {
        // 1-parameter indexer
        SetupIndexerGetter<T, TProp>(mock, impl, prop, [typeof(TIndex)]);
        SetupIndexerSetter<T, TProp>(mock, impl, prop, [typeof(TIndex)]);
    }

    /// <summary>
    /// Sets up an indexer with two index parameters.
    /// Delegates to getter and setter setup methods with the appropriate type information.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TIndex1">The type of the first index parameter.</typeparam>
    /// <typeparam name="TIndex2">The type of the second index parameter.</typeparam>
    /// <typeparam name="TProp">The property type of the indexer.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance.</param>
    /// <param name="prop">The property info representing the indexer.</param>
    private static void SetupIndexerTyped<T, TIndex1, TIndex2, TProp>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop)
        where T : class
    {
        // 2-parameter indexer
        SetupIndexerGetter<T, TProp>(mock, impl, prop, [typeof(TIndex1), typeof(TIndex2)]);
        SetupIndexerSetter<T, TProp>(mock, impl, prop, [typeof(TIndex1), typeof(TIndex2)]);
    }

    /// <summary>
    /// Sets up the getter for an indexer property to forward calls to the implementation.
    /// Builds an expression tree that matches any index parameter values and returns the value from the implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TProp">The property type of the indexer.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward getter calls to.</param>
    /// <param name="prop">The property info representing the indexer.</param>
    /// <param name="indexTypes">The types of the index parameters.</param>
    private static void SetupIndexerGetter<T, TProp>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop,
        Type[] indexTypes)
        where T : class
    {
        if (prop is not { CanRead: true, GetMethod: not null })
        {
            return;
        }

        // Build expression: m => m[It.IsAny<TIndex1>(), It.IsAny<TIndex2>(), ...]
        var paramExpr = Expression.Parameter(typeof(T), "m");

        var indexArgExprs = indexTypes
            .Select(Expression (indexType) =>
            {
                var isAnyMethod = OpenGenericMockItIsAnyMethod.MakeGenericMethod(indexType);
                return Expression.Call(isAnyMethod);
            })
            .ToArray();

        var indexerAccessExpr = Expression.MakeIndex(paramExpr, prop, indexArgExprs);

        // Build the lambda type: Func<T, TProp>
        var funcType = Expression.GetFuncType(typeof(T), typeof(TProp));
        var lambdaExpr = Expression.Lambda(funcType, indexerAccessExpr, paramExpr);

        // Call SetupGet
        var setupGetMethod =
            typeof(Mock<T>)
                .GetMethods()
                .First(m => m is { Name: nameof(Mock<object>.SetupGet), IsGenericMethodDefinition: true })
                .MakeGenericMethod(typeof(TProp));

        var setup = setupGetMethod.Invoke(mock, [lambdaExpr])!;

        // Build Returns delegate that takes index parameters and returns TProp
        var returnsDelegateType = Expression.GetFuncType(indexTypes.Concat([typeof(TProp)]).ToArray());
        var indexParamExprs =
            indexTypes
                .Select((t, i) => Expression.Parameter(t, $"idx{i + 1}"))
                .ToArray();

        var indexArgsArrayExpr = Expression.NewArrayInit(
            typeof(object),
            indexParamExprs.Select(p => Expression.Convert(p, typeof(object))));

        var getValueExpr = Expression.Call(
            Expression.Constant(prop),
            typeof(PropertyInfo).GetMethod(nameof(PropertyInfo.GetValue), [typeof(object), typeof(object[])])!,
            Expression.Constant(impl),
            indexArgsArrayExpr);

        var convertedExpr = Expression.Convert(getValueExpr, typeof(TProp));
        var returnsLambda = Expression.Lambda(returnsDelegateType, convertedExpr, indexParamExprs);
        var returnsDelegate = returnsLambda.Compile();

        // Call Returns on setup
        var returnsMethod =
            setup
                .GetType()
                .GetMethods()
                .FirstOrDefault(m =>
                {
                    if (m.Name != nameof(ISetupGetter<object, object>.Returns))
                    {
                        return false;
                    }

                    var mParams = m.GetParameters();
                    return mParams.Length == 1 && mParams[0].ParameterType.IsInstanceOfType(returnsDelegate);
                });

        returnsMethod?.Invoke(setup, [returnsDelegate]);
    }

    /// <summary>
    /// Sets up the setter for an indexer property to forward calls to the implementation.
    /// Builds an expression tree that matches any index parameter values and forwards the set value to the implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TProp">The property type of the indexer.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward setter calls to.</param>
    /// <param name="prop">The property info representing the indexer.</param>
    /// <param name="indexTypes">The types of the index parameters.</param>
    private static void SetupIndexerSetter<T, TProp>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop,
        Type[] indexTypes)
        where T : class
    {
        if (prop is not { CanWrite: true, SetMethod: not null })
        {
            return;
        }

        // For indexers, SetupSet expects Expression<Func<T, TProperty>> (indexer access, not assignment)
        var paramExpr = Expression.Parameter(typeof(T), "m");

        var indexArgExprs = indexTypes
            .Select(Expression (indexType) =>
            {
                var isAnyMethod = OpenGenericMockItIsAnyMethod.MakeGenericMethod(indexType);
                return Expression.Call(isAnyMethod);
            })
            .ToArray();

        var indexerAccessExpr = Expression.MakeIndex(paramExpr, prop, indexArgExprs);

        // Build lambda: m => m[It.IsAny<...>()] (NOT an assignment)
        var funcType = Expression.GetFuncType(typeof(T), typeof(TProp));
        var lambdaExpr = Expression.Lambda(funcType, indexerAccessExpr, paramExpr);

        // Call the obsolete SetupSet<TProperty> extension method
        var setupSetMethod =
            typeof(Mock<T>)
                .GetMethods()
                .FirstOrDefault(m =>
                {
                    if (m.Name != nameof(Mock<object>.SetupSet))
                    {
                        return false;
                    }

                    var mParams = m.GetParameters();

                    return mParams.Length == 1
                           && mParams[0].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>);
                });

        if (setupSetMethod == null)
        {
            return;
        }

        var setup = setupSetMethod.Invoke(mock, [lambdaExpr])!;

        // Build Callback delegate: Action<TIndex1, ..., TProp>
        var callbackTypes = indexTypes.Concat([typeof(TProp)]).ToArray();
        var callbackActionType = Expression.GetActionType(callbackTypes);
        var callbackParamExprs = callbackTypes.Select((t, i) =>
            i < indexTypes.Length
                ? Expression.Parameter(t, $"idx{i + 1}")
                : Expression.Parameter(t, "value")).ToArray();

        var indexArgsArrayExpr = Expression.NewArrayInit(
            typeof(object),
            callbackParamExprs
                .Take(indexTypes.Length)
                .Select(p => Expression.Convert(p, typeof(object))));

        var setValueExpr = Expression.Call(
            Expression.Constant(prop),
            typeof(PropertyInfo).GetMethod(
                nameof(PropertyInfo.SetValue),
                [typeof(object), typeof(object), typeof(object[])])!,
            Expression.Constant(impl),
            Expression.Convert(callbackParamExprs.Last(), typeof(object)),
            indexArgsArrayExpr);

        var callbackLambda = Expression.Lambda(callbackActionType, setValueExpr, callbackParamExprs);
        var callbackDelegate = callbackLambda.Compile();

        // Call Callback on setup
        var callbackMethod =
            setup
                .GetType()
                .GetMethods()
                .FirstOrDefault(m =>
                {
                    if (m.Name != nameof(ISetupSetter<object, object>.Callback))
                    {
                        return false;
                    }

                    var mParams = m.GetParameters();

                    return mParams.Length == 1
                           && mParams[0].ParameterType.IsInstanceOfType(callbackDelegate);
                });

        callbackMethod?.Invoke(setup, [callbackDelegate]);
    }

    /// <summary>
    /// Sets up a regular (non-indexer) property on the mock to forward to the implementation.
    /// Uses reflection to invoke the generic setup method with the property's type.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward property access to.</param>
    /// <param name="prop">The property info representing the regular property.</param>
    private static void SetupRegularProperty<T>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop)
        where T : class
    {
        var closedGenericSetupPropMethod =
            OpenGenericSetupPropMethod.MakeGenericMethod(typeof(T), prop.PropertyType);

        closedGenericSetupPropMethod.Invoke(null, [mock, impl, prop]);
    }

    /// <summary>
    /// Sets up a regular (non-indexer) property with strongly-typed getter and setter forwarding.
    /// Delegates to specialized getter and setter setup methods.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TProp">The property type.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward property access to.</param>
    /// <param name="prop">The property info representing the regular property.</param>
    private static void SetupRegularProperty<T, TProp>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop)
        where T : class
    {
        SetupRegularPropertyGetter<T, TProp>(mock, impl, prop);
        SetupRegularPropertySetter<T, TProp>(mock, impl, prop);
    }

    /// <summary>
    /// Sets up the getter for a regular property to forward calls to the implementation.
    /// Creates a strongly-typed expression that retrieves the property value from the implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TProp">The property type.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward getter calls to.</param>
    /// <param name="prop">The property info representing the property.</param>
    private static void SetupRegularPropertyGetter<T, TProp>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop)
        where T : class
    {
        if (prop is not { CanRead: true, GetMethod: not null })
        {
            return;
        }

        var paramExpr = Expression.Parameter(typeof(T), "m");
        var propAccessExpr = Expression.Property(paramExpr, prop);
        var lambdaExpr = Expression.Lambda<Func<T, TProp>>(propAccessExpr, paramExpr);

        mock.SetupGet(lambdaExpr)
            .Returns(() => (TProp)prop.GetValue(impl)!);
    }

    /// <summary>
    /// Sets up the setter for a regular property to forward calls to the implementation.
    /// Creates a callback that applies the set value to the implementation's property.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TProp">The property type.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward setter calls to.</param>
    /// <param name="prop">The property info representing the property.</param>
    private static void SetupRegularPropertySetter<T, TProp>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop)
        where T : class
    {
        if (prop is not { CanWrite: true, SetMethod: not null })
        {
            return;
        }

        mock.SetupSet(m => prop.SetValue(m, It.IsAny<TProp>()))
            .Callback<TProp>(value => prop.SetValue(impl, value));
    }
}