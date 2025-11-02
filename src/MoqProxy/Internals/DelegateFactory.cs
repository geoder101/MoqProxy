// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using System.Linq.Expressions;
using System.Reflection;

namespace MoqProxy.Internals;

/// <summary>
/// Provides methods for creating delegates that invoke methods on implementation instances.
/// </summary>
internal static class DelegateFactory
{
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
    /// Creates a callback delegate that invokes a method on the implementation instance.
    /// Builds an expression tree that packages method parameters into an array and calls the method via reflection.
    /// </summary>
    /// <param name="impl">The implementation instance to invoke the method on.</param>
    /// <param name="method">The method to invoke.</param>
    /// <param name="paramTypes">The parameter types of the method.</param>
    /// <returns>A delegate that can be used with Moq's Callback method.</returns>
    internal static Delegate CreateCallbackDelegate(
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
    internal static Delegate CreateReturnsDelegate(
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
}