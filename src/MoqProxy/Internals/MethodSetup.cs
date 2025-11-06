// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using System.Linq.Expressions;
using System.Reflection;
using Moq;
using Moq.Language.Flow;

namespace MoqProxy.Internals;

/// <summary>
/// Provides methods for setting up method proxying on mock instances.
/// </summary>
internal static class MethodSetup
{
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
    /// Sets up all public instance methods on the mock to forward to the implementation.
    /// Skips special methods (property accessors, operators), Object methods, generic methods with unresolved type parameters,
    /// methods with ref/out parameters (handled entirely by the interceptor), and methods with ref-like parameters (ref structs)
    /// which are not supported by expression trees.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward method calls to.</param>
    internal static void SetupMethods<T>(
        Mock<T> mock,
        T impl)
        where T : class
    {
        foreach (var method in typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            // Skip some generic methods here - they're handled separately
            // Skip methods with ref/out parameters (IsByRef) — these cannot be represented in expression trees
            // and are handled entirely by the ProxyInterceptor
            // Skip methods with ref-like parameters (IsByRefLike, ref struct) — not supported by expression trees
            if (method.IsSpecialName
                || method.DeclaringType == typeof(object)
                || method.ReturnType.ContainsGenericParameters
                || method.GetParameters().Any(p => p.ParameterType.IsByRef || p.ParameterType.IsByRefLike))
            {
                continue;
            }

            SetupMethod(mock, impl, method);
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
            var callbackDelegate = DelegateFactory.CreateCallbackDelegate(impl, method, methodParamTypes);

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
            var returnsDelegate = DelegateFactory.CreateReturnsDelegate(impl, method, methodParamTypes);

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
}