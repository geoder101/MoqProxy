// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

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
    /// <item><description>Forward property getters and setters to the implementation</description></item>
    /// <item><description>Forward method calls to the implementation</description></item>
    /// <item><description>Support indexer properties</description></item>
    /// <item><description>Handle generic methods through fallback interceptors</description></item>
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

        SetupProperties(mock, impl);
        SetupMethods(mock, impl);
        SetupMethodFallbackInterceptors(mock, impl);
    }

    #region Core

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

    private static readonly MethodInfo OpenGenericMockItIsAnyMethod =
        typeof(It).GetMethod(nameof(It.IsAny))
        ?? throw new InvalidOperationException(
            $"""
             Failed to find method '{nameof(It.IsAny)}' on type '{typeof(It).FullName}'. 
             This may indicate an incompatible version of Moq library.
             """);

    private static readonly MethodInfo MethodInfoInvokeMethod =
        typeof(MethodInfo).GetMethod(
            nameof(MethodInfo.Invoke),
            [typeof(object), typeof(object[])])
        ?? throw new InvalidOperationException(
            $"Failed to find method '{nameof(MethodInfo.Invoke)}' on type '{typeof(MethodInfo).FullName}'.");

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
            .Where(m => m.Name == nameof(SetupIndexerTyped) && m.IsGenericMethodDefinition)
            .FirstOrDefault(m => m.GetGenericArguments().Length == allTypes.Length + 1); // +1 for T

        if (helperMethod == null)
        {
            // Unsupported indexer arity (more than 2 parameters)
            return;
        }

        helperMethod = helperMethod.MakeGenericMethod([typeof(T), .. allTypes]);
        helperMethod.Invoke(null, [mock, impl, prop]);
    }

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
            .Select(indexType =>
            {
                var isAnyMethod = OpenGenericMockItIsAnyMethod.MakeGenericMethod(indexType);
                return (Expression)Expression.Call(isAnyMethod);
            })
            .ToArray();

        var indexerAccessExpr = Expression.MakeIndex(paramExpr, prop, indexArgExprs);

        // Build the lambda type: Func<T, TProp>
        var funcType = Expression.GetFuncType(typeof(T), typeof(TProp));
        var lambdaExpr = Expression.Lambda(funcType, indexerAccessExpr, paramExpr);

        // Call SetupGet
        var setupGetMethod = typeof(Mock<T>)
            .GetMethods()
            .First(m => m.Name == nameof(Mock<T>.SetupGet) && m.IsGenericMethodDefinition)
            .MakeGenericMethod(typeof(TProp));

        var setup = setupGetMethod.Invoke(mock, [lambdaExpr])!;

        // Build Returns delegate that takes index parameters and returns TProp
        var returnsDelegateType = Expression.GetFuncType(indexTypes.Concat([typeof(TProp)]).ToArray());
        var indexParamExprs = indexTypes.Select((t, i) => Expression.Parameter(t, $"idx{i}")).ToArray();

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
        var returnsMethod = setup.GetType()
            .GetMethods()
            .FirstOrDefault(m =>
            {
                if (m.Name != "Returns")
                {
                    return false;
                }

                var mParams = m.GetParameters();
                return mParams.Length == 1 && mParams[0].ParameterType.IsInstanceOfType(returnsDelegate);
            });

        returnsMethod?.Invoke(setup, [returnsDelegate]);
    }

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
            .Select(indexType =>
            {
                var isAnyMethod = OpenGenericMockItIsAnyMethod.MakeGenericMethod(indexType);
                return (Expression)Expression.Call(isAnyMethod);
            })
            .ToArray();

        var indexerAccessExpr = Expression.MakeIndex(paramExpr, prop, indexArgExprs);

        // Build lambda: m => m[It.IsAny<...>()] (NOT an assignment)
        var funcType = Expression.GetFuncType(typeof(T), typeof(TProp));
        var lambdaExpr = Expression.Lambda(funcType, indexerAccessExpr, paramExpr);

        // Call the obsolete SetupSet<TProperty> extension method
        var setupSetMethod = typeof(Mock<T>)
            .GetMethods()
            .FirstOrDefault(m => m.Name == "SetupSet" && m.GetParameters().Length == 1
                                                      && m.GetParameters()[0].ParameterType.IsGenericType
                                                      && m.GetParameters()[0].ParameterType
                                                          .GetGenericTypeDefinition() == typeof(Expression<>));

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
                ? Expression.Parameter(t, $"idx{i}")
                : Expression.Parameter(t, "value")).ToArray();

        var indexArgsArrayExpr = Expression.NewArrayInit(
            typeof(object),
            callbackParamExprs.Take(indexTypes.Length).Select(p => Expression.Convert(p, typeof(object))));

        var setValueExpr = Expression.Call(
            Expression.Constant(prop),
            typeof(PropertyInfo).GetMethod(nameof(PropertyInfo.SetValue),
                [typeof(object), typeof(object), typeof(object[])])!,
            Expression.Constant(impl),
            Expression.Convert(callbackParamExprs.Last(), typeof(object)),
            indexArgsArrayExpr);

        var callbackLambda = Expression.Lambda(callbackActionType, setValueExpr, callbackParamExprs);
        var callbackDelegate = callbackLambda.Compile();

        // Call Callback on setup
        var callbackMethod = setup.GetType()
            .GetMethods()
            .FirstOrDefault(m =>
            {
                if (m.Name != "Callback")
                {
                    return false;
                }

                var mParams = m.GetParameters();
                return mParams.Length == 1 && mParams[0].ParameterType.IsInstanceOfType(callbackDelegate);
            });

        callbackMethod?.Invoke(setup, [callbackDelegate]);
    }

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

    private static void SetupRegularProperty<T, TProp>(
        Mock<T> mock,
        T impl,
        PropertyInfo prop)
        where T : class
    {
        SetupRegularPropertyGetter<T, TProp>(mock, impl, prop);
        SetupRegularPropertySetter<T, TProp>(mock, impl, prop);
    }

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

    private static void SetupMethodFallbackInterceptors<T>(
        Mock<T> mock,
        T impl)
        where T : class
    {
        // Expression-tree setups in Moq cannot represent certain generic methods whose signatures depend on invariant type parameters.
        // Castle.DynamicProxy (used by Moq) can still intercept those generic calls at runtime.
        // We inject a custom interceptor into the generated proxy (via reflection) to handle forwarding generic invocations to the real implementation.

        if (mock.Object is not IProxyTargetAccessor)
        {
            return;
        }

        // Create our custom interceptor for generic methods
        var fallbackProxyInterceptor = new FallbackMethodProxyInterceptor<T>(impl);

        // Use reflection to access the __interceptors field (Castle DynamicProxy implementation detail)
        var proxyType = mock.Object.GetType();
        var interceptorsField = proxyType.GetField("__interceptors", BindingFlags.NonPublic | BindingFlags.Instance);

        if (interceptorsField == null)
        {
            return;
        }

        var currentInterceptors = (IInterceptor[])interceptorsField.GetValue(mock.Object)!;
        var newInterceptors = new[] { fallbackProxyInterceptor }.Concat(currentInterceptors).ToArray();
        interceptorsField.SetValue(mock.Object, newInterceptors);
    }

    private class FallbackMethodProxyInterceptor<T>(T impl)
        : IInterceptor
        where T : class
    {
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

    private sealed class NullReturnValue
    {
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

    private static void SetupMethod<T>(
        Mock<T> mock,
        T impl,
        MethodInfo method)
        where T : class
    {
        var isVoid = method.ReturnType == typeof(void);
        var isGeneric = method.IsGenericMethod;

        var methodParams = method.GetParameters();

        var methodParamTypes =
            methodParams
                .Select(p => p.ParameterType)
                .Select(t =>
                    // Erase generic types by substituting with 'object'
                    t.IsGenericMethodParameter ? typeof(object) : t)
                .ToArray();

        // Build the expression tree for mock.Setup(m => m.Method(It.IsAny<T1>(), It.IsAny<T2>(), ...))
        var methodCallParamExpr = Expression.Parameter(typeof(T), "m");

        // Create It.IsAny<T>() calls for each parameter
        var methodParamExprs =
            methodParamTypes
                .Select(Expression (paramType) =>
                {
                    paramType = paramType.IsGenericMethodParameter ? typeof(object) : paramType;
                    var closedGenericMockItIsAnyMethod = OpenGenericMockItIsAnyMethod.MakeGenericMethod(paramType);
                    return Expression.Call(closedGenericMockItIsAnyMethod);
                })
                .ToArray();

        if (isGeneric)
        {
            method = method.MakeGenericMethod(methodParamTypes);
        }

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

            // Erase generic return type by substituting with 'object'
            var returnType = isGeneric ? typeof(object) : method.ReturnType;

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
            paramTypes.Select((t, i) => Expression.Parameter(t, $"a{i}")).ToArray();

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

    private class NullReturnValueProvider : DefaultValueProvider
    {
        protected override object GetDefaultValue(Type type, Mock mock)
        {
            return NullReturnValue.Instance;
        }
    }

    #endregion
}