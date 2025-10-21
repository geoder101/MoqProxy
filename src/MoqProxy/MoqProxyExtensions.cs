// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;
using Moq;
using Moq.Language.Flow;
using IInvocation = Castle.DynamicProxy.IInvocation;

namespace MoqProxy;

/// <summary>
/// Provides extension methods for configuring Moq mocks to act as proxies that forward calls to real implementations.
/// </summary>
public static class MoqProxyExtensions
{
    /// <summary>
    /// Sets up a mock to act as a proxy by forwarding all calls to the provided implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked. Must be a class.</typeparam>
    /// <param name="mock">The Moq mock instance to configure as a proxy.</param>
    /// <param name="impl">The implementation instance to which calls will be forwarded.</param>
    /// <remarks>
    /// This method configures the mock to:
    /// <list type="bullet">
    /// <item><description>Inject a fallback interceptor to forward unmatched calls to the implementation</description></item>
    /// <item><description>Forward property getters and setters to the implementation</description></item>
    /// <item><description>Forward method calls to the implementation</description></item>
    /// <item><description>Support indexer properties</description></item>
    /// </list>
    /// After calling this method, you can still override specific behaviors using standard Moq Setup methods.
    /// </remarks>
    public static void SetupAsProxy<T>(
        this Mock<T> mock,
        T impl)
        where T : class
    {
        // Set up custom default value provider to return NullReturnValue sentinel
        mock.DefaultValueProvider = new NullReturnValueProvider();

        // Inject the fallback interceptor first - it runs before all other setups
        if (!TrySetupInterceptor(mock, impl))
        {
            return;
        }

        SetupProperties(mock, impl);
        SetupMethods(mock, impl);
    }

    #region Core

    /// <summary>
    /// Cached reflection reference to the generic <see cref="SetupRegularProperty{T, TProp}"/> method.
    /// Used to invoke the method with runtime type arguments for property setup.
    /// </summary>
    private static readonly MethodInfo OpenGenericSetupPropMethod =
        typeof(MoqProxyExtensions)
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
             Failed to find method '{nameof(SetupRegularProperty)}<T, TProp>' on type '{typeof(MoqProxyExtensions).FullName}'.
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
    /// Cached reflection reference to the MethodInfo.Invoke method.
    /// Used to dynamically invoke methods on the implementation instance.
    /// </summary>
    private static readonly MethodInfo MethodInfoInvokeMethod =
        typeof(MethodInfo).GetMethod(
            nameof(MethodInfo.Invoke),
            [typeof(object), typeof(object[])])
        ?? throw new InvalidOperationException(
            $"Failed to find method '{nameof(MethodInfo.Invoke)}' on type '{typeof(MethodInfo).FullName}'.");

    /// <summary>
    /// Sets up all public instance properties on the mock to forward to the implementation.
    /// Distinguishes between regular properties and indexers, delegating to appropriate setup methods.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward property access to.</param>
    private static void SetupProperties<T>(
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
        var helperMethod = typeof(MoqProxyExtensions)
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

    /// <summary>
    /// Sets up all public instance methods on the mock to forward to the implementation.
    /// Skips special methods (property accessors, operators), Object methods, generic methods with unresolved type parameters,
    /// and methods with by-ref or ref-like parameters which are not supported by this proxy.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward method calls to.</param>
    private static void SetupMethods<T>(
        Mock<T> mock,
        T impl)
        where T : class
    {
        foreach (var method in typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            // Skip some generic methods here - they're handled separately
            // Skip methods with by-ref (ref/out) or ref-like parameters (ref struct) — not supported by this proxy
            if (method.IsSpecialName
                || method.DeclaringType == typeof(object)
                || method.ReturnType.ContainsGenericParameters
                || method.GetParameters().Any(p =>
                    p.ParameterType.IsByRef
                    || p.ParameterType.IsByRefLike))
            {
                continue;
            }

            SetupMethod(mock, impl, method);
        }
    }

    /// <summary>
    /// Attempts to inject a custom interceptor into the mock's proxy that forwards unmatched calls to the implementation.
    /// This interceptor runs before all Moq setups, detecting when no setup was matched and forwarding the call to the real implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward calls to.</param>
    /// <remarks>
    /// The interceptor is only added once. If it's already present, this method does nothing.
    /// Uses Castle.DynamicProxy's internal __interceptors field to inject the custom interceptor at the beginning of the chain.
    /// </remarks>
    private static bool TrySetupInterceptor<T>(
        Mock<T> mock,
        T impl)
        where T : class
    {
        if (mock.Object is not IProxyTargetAccessor)
        {
            return false;
        }

        // Use reflection to access the __interceptors field (Castle DynamicProxy implementation detail)
        var proxyType = mock.Object.GetType();
        var interceptorsField = proxyType.GetField("__interceptors", BindingFlags.NonPublic | BindingFlags.Instance);

        if (interceptorsField == null)
        {
            return false;
        }

        var currentInterceptors = (IInterceptor[])interceptorsField.GetValue(mock.Object)!;

        // Check if our interceptor is already added - skip if it is
        if (currentInterceptors.Any(i => i is FallbackMethodProxyInterceptor<T>))
        {
            return false;
        }

        // Create our custom interceptor
        var fallbackProxyInterceptor = new FallbackMethodProxyInterceptor<T>(impl);

        // Prepend our interceptor to the beginning of the chain so it runs first
        var newInterceptors = new[] { fallbackProxyInterceptor }.Concat(currentInterceptors).ToArray();
        interceptorsField.SetValue(mock.Object, newInterceptors);

        return true;
    }

    /// <summary>
    /// Castle.DynamicProxy interceptor that forwards method calls to the real implementation when no Moq setup matches.
    /// Uses a sentinel value to detect when Moq hasn't matched any setup, then invokes the method on the real implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="impl">The implementation instance to forward calls to.</param>
    private class FallbackMethodProxyInterceptor<T>(T impl)
        : IInterceptor
        where T : class
    {
        /// <summary>
        /// Intercepts method calls on the mock proxy, checking if a Moq setup was matched.
        /// If no setup matched (indicated by the sentinel return value), forwards the call to the real implementation.
        /// </summary>
        /// <param name="invocation">The method invocation details from Castle.DynamicProxy.</param>
        public void Intercept(IInvocation invocation)
        {
            var method = invocation.Method;

            // Use NullReturnValue as sentinel to detect if no setup was matched
            if (method.ReturnType != typeof(void))
            {
                invocation.ReturnValue = NullReturnValue.Instance;
            }

            Exception exception1 = null!;
            Exception exception2 = null!;

            try
            {
                invocation.Proceed();
            }
            catch (Exception ex)
            {
                exception1 = ex;
            }
            finally
            {
                try
                {
                    if (method.ReturnType != typeof(void))
                    {
                        // If ReturnValue is still NullReturnValue, it means no Setup was matched
                        if (invocation.ReturnValue == NullReturnValue.Instance)
                        {
                            invocation.ReturnValue = method.Invoke(impl, invocation.Arguments);
                        }
                    }
                }
                catch (Exception ex)
                {
                    exception2 = ex;
                }
            }

            switch (exception1, exception2)
            {
                case (not null, not null):
                    throw new AggregateException(
                        "Multiple exceptions occurred during method interception.",
                        exception1, exception2);
                case (not null, null): throw exception1;
                case (null, not null): throw exception2;
            }
        }
    }

    /// <summary>
    /// Sets up a single method on the mock to forward calls to the implementation.
    /// Handles both void and non-void methods, creating appropriate Setup/Callback or Setup/Returns configurations.
    /// For generic methods, attempts to erase generic parameters to create a concrete method definition.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward method calls to.</param>
    /// <param name="method">The method info to set up.</param>
    private static void SetupMethod<T>(
        Mock<T> mock,
        T impl,
        MethodInfo method)
        where T : class
    {
        var isVoid = method.ReturnType == typeof(void);
        var isGeneric = method.IsGenericMethod;

        // For generic methods, construct the concrete method first before processing parameters
        if (isGeneric)
        {
            if (!method.TryEraseGenericParameters(out var concreteMethod))
            {
                // Can't satisfy generic parameter constraints with the chosen types -> skip setup
                return;
            }

            method = concreteMethod;
        }

        var methodParams = method.GetParameters();

        var methodParamTypes =
            methodParams
                .Select(p => p.ParameterType)
                .Select(t => t.EraseGenericParameters())
                .ToArray();

        // Build the expression tree for mock.Setup(m => m.Method(It.IsAny<T1>(), It.IsAny<T2>(), ...))
        var methodCallParamExpr = Expression.Parameter(typeof(T), "m");

        // Create It.IsAny<T>() calls for each parameter
        var methodParamExprs =
            methodParamTypes
                .Select(Expression (paramType) =>
                {
                    paramType = paramType.EraseGenericParameters();
                    var closedGenericMockItIsAnyMethod = OpenGenericMockItIsAnyMethod.MakeGenericMethod(paramType);
                    return Expression.Call(closedGenericMockItIsAnyMethod);
                })
                .ToArray();

        var methodCallExpr = Expression.Call(methodCallParamExpr, method, methodParamExprs);

        if (isVoid)
        {
            // For void methods: mock.Setup(m => m.Method(...)).Callback(...)

            var setupLambdaExpr =
                Expression.Lambda<Action<T>>(methodCallExpr, methodCallParamExpr);

            var setup = mock.Setup(setupLambdaExpr);

            // Create callback delegate
            var callbackDelegate = CreateCallbackDelegate(impl, method, methodParamTypes);

            // Get the Callback method with the right signature
            var callbackMethod =
                setup
                    .GetType()
                    .GetMethods()
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != nameof(ISetup<object>.Callback))
                        {
                            return false;
                        }

                        var mParams = m.GetParameters();

                        return mParams.Length == 1
                               && mParams[0].ParameterType.IsInstanceOfType(callbackDelegate);
                    });

            callbackMethod?.Invoke(setup, [callbackDelegate]);
        }
        else
        {
            // For returning methods: mock.Setup(m => m.Method(...)).Returns(...)

            var returnType =
                isGeneric && !method.ReturnType.IsValueType
                    ? typeof(object)
                    : method.ReturnType;

            var funcType = Expression.GetFuncType(typeof(T), returnType);

            var setupLambdaMethod =
                typeof(Expression)
                    .GetMethods()
                    .First(m =>
                        m is { Name: nameof(Expression.Lambda), IsGenericMethodDefinition: true }
                        && m.GetParameters().Length == 2)
                    .MakeGenericMethod(funcType);

            var setupLambda =
                setupLambdaMethod.Invoke(null, [methodCallExpr, new[] { methodCallParamExpr }]);

            // Call mock.Setup<TResult>(expression)
            var setupMethod =
                typeof(Mock<T>)
                    .GetMethods()
                    .First(m =>
                        m is { Name: nameof(Mock<object>.Setup), IsGenericMethodDefinition: true }
                        && m.GetParameters().Length == 1)
                    .MakeGenericMethod(returnType);

            var setup = setupMethod.Invoke(mock, [setupLambda])!;

            // Create returns delegate
            var returnsDelegate = CreateReturnsDelegate(impl, method, methodParamTypes);

            // Get the Returns method with the right signature
            var returnsMethod =
                setup
                    .GetType()
                    .GetMethods()
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != nameof(ISetup<object, object>.Returns))
                        {
                            return false;
                        }

                        var mParameters = m.GetParameters();

                        return mParameters.Length == 1
                               && mParameters[0].ParameterType.IsAssignableTo(typeof(Delegate));
                    });

            returnsMethod?.Invoke(setup, [returnsDelegate]);
        }
    }

    /// <summary>
    /// Creates a callback delegate that invokes a method on the implementation instance.
    /// Builds an expression tree that packages method parameters into an array and calls the method via reflection.
    /// </summary>
    /// <param name="impl">The implementation instance to invoke the method on.</param>
    /// <param name="method">The method to invoke.</param>
    /// <param name="paramTypes">The parameter types of the method.</param>
    /// <returns>A delegate that can be used with Moq's Callback method.</returns>
    private static Delegate CreateCallbackDelegate(
        object impl,
        MethodInfo method,
        Type[] paramTypes)
    {
        // Create Action or Action<T1, T2, ...> delegate
        var actionType = Expression.GetActionType(paramTypes);

        var paramExprs =
            paramTypes
                .Select((t, i) => Expression.Parameter(t, $"a{i + 1}"))
                .ToArray();

        var argsArrayExpr =
            Expression.NewArrayInit(
                typeof(object),
                paramExprs.Select(p => Expression.Convert(p, typeof(object))));

        var invokeCallExpr =
            Expression.Call(
                Expression.Constant(method),
                MethodInfoInvokeMethod,
                Expression.Constant(impl),
                argsArrayExpr);

        var lambdaExpr = Expression.Lambda(actionType, invokeCallExpr, paramExprs);
        return lambdaExpr.Compile();
    }

    /// <summary>
    /// Creates a returns delegate that invokes a method on the implementation instance and returns its result.
    /// Builds an expression tree that packages method parameters into an array, calls the method via reflection, and returns the result.
    /// </summary>
    /// <param name="impl">The implementation instance to invoke the method on.</param>
    /// <param name="method">The method to invoke.</param>
    /// <param name="paramTypes">The parameter types of the method.</param>
    /// <returns>A delegate that can be used with Moq's Returns method.</returns>
    private static Delegate CreateReturnsDelegate(
        object impl,
        MethodInfo method,
        Type[] paramTypes)
    {
        var returnType = method.ReturnType;

        // Create Func<T1, T2, ..., TResult> delegate
        var funcType =
            Expression.GetFuncType(paramTypes.Concat([returnType]).ToArray());

        var paramExprs =
            paramTypes
                .Select((t, i) => Expression.Parameter(t, $"a{i + 1}"))
                .ToArray();

        if (paramTypes.Length == 0)
        {
            var invokeCallExpr =
                Expression.Call(
                    Expression.Constant(method),
                    MethodInfoInvokeMethod,
                    Expression.Constant(impl),
                    Expression.Constant(null, typeof(object[])));

            var convertedCallExpr = Expression.Convert(invokeCallExpr, returnType);
            var lambdaExpr = Expression.Lambda(funcType, convertedCallExpr, paramExprs);
            return lambdaExpr.Compile();
        }
        else
        {
            var argsArrayExpr =
                Expression.NewArrayInit(
                    typeof(object),
                    paramExprs.Select(p => Expression.Convert(p, typeof(object))));

            var invokeCallExpr =
                Expression.Call(
                    Expression.Constant(method),
                    MethodInfoInvokeMethod,
                    Expression.Constant(impl),
                    argsArrayExpr);

            var convertedCall = Expression.Convert(invokeCallExpr, returnType);
            var lambda = Expression.Lambda(funcType, convertedCall, paramExprs);
            return lambda.Compile();
        }
    }

    /// <summary>
    /// Replaces any open generic parameters in a <see cref="Type"/> with <see cref="object"/>.
    /// This produces an "erased" type that can be used when building reflection-based
    /// expressions or constructing closed generic types for mocks.
    /// </summary>
    /// <param name="type">The type to erase generic parameters from.</param>
    /// <returns>
    /// A type with all generic parameters replaced by <see cref="object"/>, preserving array, pointer, and by-ref shapes.
    /// </returns>
    /// <remarks>
    /// The method:
    /// <list type="bullet">
    /// <item><description>Maps generic type parameters to <see cref="object"/></description></item>
    /// <item><description>Preserves arrays, pointers and by-ref shapes while erasing their element types</description></item>
    /// <item><description>Rebuilds generic types using erased type arguments</description></item>
    /// </list>
    /// </remarks>
    private static Type EraseGenericParameters(this Type type)
    {
        // If the type itself is a generic parameter (e.g. T) use object.
        if (type.IsGenericParameter
            || type.IsGenericMethodParameter)
        {
            return typeof(object);
        }

        // Handle arrays: erase the element type then re-create the array with the original rank.
        if (type.IsArray)
        {
            var elem = type.GetElementType()!.EraseGenericParameters();
            var rank = type.GetArrayRank();
            // Use MakeArrayType() for single-dimensional zero-based arrays, MakeArrayType(rank) for multi-dimensional
            return rank == 1 ? elem.MakeArrayType() : elem.MakeArrayType(rank);
        }

        // Handle by-ref types (ref/out): erase the element and return a by-ref of it.
        if (type.IsByRef)
        {
            var elem = type.GetElementType()!.EraseGenericParameters();
            return elem.MakeByRefType();
        }

        // Handle pointers similarly.
        if (type.IsPointer)
        {
            var elem = type.GetElementType()!.EraseGenericParameters();
            return elem.MakePointerType();
        }

        // Non-generic types are returned as-is.
        if (!type.IsGenericType)
        {
            return type;
        }

        var def = type.GetGenericTypeDefinition();

        // Special case: Nullable<T> where T is a generic parameter
        // Nullable<T> has a struct constraint, so we can't make Nullable<object>
        // Check this BEFORE erasing the generic arguments to avoid attempting invalid construction
        if (def == typeof(Nullable<>))
        {
            var arg = type.GetGenericArguments()[0];
            if (arg.IsGenericParameter || arg.IsGenericMethodParameter)
            {
                return typeof(object);
            }
        }

        // For generic types, erase each generic argument and construct the generic type definition
        // with the erased arguments.
        var newArgs = type.GetGenericArguments()
            .Select(a => a.EraseGenericParameters())
            .ToArray();

        // After erasure, check if we would create Nullable<object>
        if (def == typeof(Nullable<>) && newArgs.Length == 1 && newArgs[0] == typeof(object))
        {
            return typeof(object);
        }

        // Try to construct the generic type, but if it fails due to constraint violations,
        // just return object as a fallback
        try
        {
            return def.MakeGenericType(newArgs);
        }
        catch (ArgumentException)
        {
            // Constraints prevent construction (e.g., other constrained generics)
            return typeof(object);
        }
    }

    /// <summary>
    /// Attempts to construct a concrete <see cref="MethodInfo"/> from a generic method definition by substituting
    /// all method generic parameters with <see cref="object"/>.
    /// </summary>
    /// <param name="method">The method to attempt to make concrete.</param>
    /// <param name="concreteMethod">When this method returns true, contains the concrete method; otherwise, null.</param>
    /// <returns>
    /// <c>true</c> if the method is a generic method definition and a concrete method could be constructed;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Returns false if the method is not a generic method definition or if generic parameter constraints
    /// prevent construction. This is used by the proxy setup to try to create a usable non-generic method
    /// for expression construction; failure is expected for some generic methods and is handled by skipping setup.
    /// </remarks>
    private static bool TryEraseGenericParameters(
        this MethodInfo method,
        [NotNullWhen(true)] out MethodInfo? concreteMethod)
    {
        // Build generic type arguments: replace any method/type generic parameters with `object`.
        var genericParams = method.GetGenericArguments();
        var genericTypeArgs = genericParams
            .Select(gp =>
                gp.IsGenericParameter || gp.IsGenericMethodParameter
                    ? typeof(object)
                    : gp)
            .ToArray();

        // Try to construct the generic method. If constraints prevent construction, skip this method.
        try
        {
            if (method.IsGenericMethodDefinition)
            {
                concreteMethod = method.MakeGenericMethod(genericTypeArgs);
                return true;
            }

            concreteMethod = null;
            return false;
        }
        catch (ArgumentException)
        {
            // Can't satisfy generic parameter constraints with the chosen types -> skip setup
            concreteMethod = null;
            return false;
        }
    }

    /// <summary>
    /// Sentinel type used to detect when Moq has not matched any setup for a method call.
    /// This singleton value is returned by the custom <see cref="NullReturnValueProvider"/> and checked
    /// by the <see cref="FallbackMethodProxyInterceptor{T}"/> to determine whether to forward the call to the real implementation.
    /// </summary>
    private sealed class NullReturnValue
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="NullReturnValue"/>.
        /// </summary>
        public static readonly NullReturnValue Instance = new();

        private NullReturnValue()
        {
        }

        public static bool operator ==(NullReturnValue? left, NullReturnValue? right)
            => ReferenceEquals(left, right) || (left is not null && right is not null);

        public static bool operator !=(NullReturnValue? left, NullReturnValue? right)
            => !(left == right);

        public static bool operator ==(NullReturnValue? left, object? right)
            => right is null || (left is not null && ReferenceEquals(left, right));

        public static bool operator !=(NullReturnValue? left, object? right)
            => !(left == right);

        public static bool operator ==(object? left, NullReturnValue? right)
            => left is null || (right is not null && ReferenceEquals(left, right));

        public static bool operator !=(object? left, NullReturnValue? right)
            => !(left == right);

        public override bool Equals(object? obj)
            => obj is null or NullReturnValue;

        public override int GetHashCode() => 0;
    }

    /// <summary>
    /// Custom Moq <see cref="DefaultValueProvider"/> that returns the <see cref="NullReturnValue"/> sentinel
    /// for all unmatched method calls. This allows the interceptor to detect when no setup was matched
    /// and forward the call to the real implementation.
    /// </summary>
    private class NullReturnValueProvider : DefaultValueProvider
    {
        /// <summary>
        /// Returns the <see cref="NullReturnValue.Instance"/> sentinel for any type.
        /// </summary>
        /// <param name="type">The return type of the method (unused).</param>
        /// <param name="mock">The mock instance (unused).</param>
        /// <returns>The <see cref="NullReturnValue.Instance"/> sentinel.</returns>
        protected override object GetDefaultValue(Type type, Mock mock)
            => NullReturnValue.Instance;
    }

    #endregion
}