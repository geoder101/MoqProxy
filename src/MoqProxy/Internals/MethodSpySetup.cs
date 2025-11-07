// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using System.Linq.Expressions;
using Moq;
using Moq.Language.Flow;

namespace MoqProxy.Internals;

/// <summary>
/// Provides internal methods for setting up spy behavior on mock methods.
/// </summary>
internal static class MethodSpySetup
{
    /// <summary>
    /// Sets up a spy on a specific void method, forwarding calls to the upstream implementation while tracking them with Moq.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock proxy instance.</param>
    /// <param name="expression">Expression specifying the method to spy on.</param>
    /// <param name="callback">Callback invoked when the method is called.</param>
    internal static void SetupVoidMethodSpy<T>(
        Mock<T> mock,
        Expression<Action<T>> expression,
        Delegate callback)
        where T : class
    {
        // Get the upstream implementation
        var impl = MockProxy.GetUpstreamInstance(mock.Object);
        if (impl == null)
        {
            throw new InvalidOperationException(
                "The mock must be configured as a proxy using SetupAsProxy before calling Spy.");
        }

        // Extract method info from the expression
        if (expression.Body is not MethodCallExpression methodCallExpr)
        {
            throw new ArgumentException(
                "Expression must be a method call.",
                nameof(expression));
        }

        var method = methodCallExpr.Method;
        var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

        // Create the callback delegate that forwards to the implementation
        var forwardDelegate = DelegateFactory.CreateCallbackDelegate(impl, method, paramTypes);

        var actionType = Expression.GetActionType(paramTypes);

        var paramExprs =
            paramTypes
                .Select((t, i) => Expression.Parameter(t, $"p{i + 1}"))
                .ToArray();

        // Call user callback - check if callback expects parameters
        Expression userCallbackExpr;
        var callbackParams = callback.Method.GetParameters();
        if (callbackParams.Length == 0)
        {
            // Parameterless callback
            userCallbackExpr = Expression.Invoke(Expression.Constant(callback));
        }
        else if (callbackParams.Length == paramTypes.Length)
        {
            // Callback with matching parameters
            // ReSharper disable once CoVariantArrayConversion
            userCallbackExpr = Expression.Invoke(Expression.Constant(callback), paramExprs);
        }
        else
        {
            throw new ArgumentException(
                $"Callback must have 0 or {paramTypes.Length} parameters.",
                nameof(callback));
        }

        // Call forward delegate
        // ReSharper disable once CoVariantArrayConversion
        var forwardCallExpr = Expression.Invoke(Expression.Constant(forwardDelegate), paramExprs);

        // Combine them in a block
        var blockExpr = Expression.Block(userCallbackExpr, forwardCallExpr);

        // Create lambda
        var lambdaExpr = Expression.Lambda(actionType, blockExpr, paramExprs);
        var combinedCallback = lambdaExpr.Compile();

        // Set up the mock with the combined callback
        var setup = mock.Setup(expression);

        var callbackMethod = setup
            .GetType()
            .GetMethods()
            .FirstOrDefault(m =>
            {
                if (m.Name != nameof(ISetup<object>.Callback))
                {
                    return false;
                }

                var mParams = m.GetParameters();
                return mParams.Length == 1 && mParams[0].ParameterType.IsInstanceOfType(combinedCallback);
            });

        callbackMethod?.Invoke(setup, [combinedCallback]);
    }

    /// <summary>
    /// Sets up a spy on a specific method that has a return value, forwarding calls to the upstream implementation while tracking them with Moq.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TResult">The return type of the method.</typeparam>
    /// <param name="mock">The mock proxy instance.</param>
    /// <param name="expression">Expression specifying the method to spy on.</param>
    /// <param name="callback">Callback invoked before the real implementation is called.</param>
    internal static void SetupReturningMethodSpy<T, TResult>(
        Mock<T> mock,
        Expression<Func<T, TResult>> expression,
        Delegate callback)
        where T : class
    {
        // Get the upstream implementation
        var impl = MockProxy.GetUpstreamInstance(mock.Object);
        if (impl == null)
        {
            throw new InvalidOperationException(
                "The mock must be configured as a proxy using SetupAsProxy before calling Spy.");
        }

        // Extract method info from the expression
        if (expression.Body is not MethodCallExpression methodCallExpr)
        {
            throw new ArgumentException(
                "Expression must be a method call.",
                nameof(expression));
        }

        var method = methodCallExpr.Method;
        var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

        // Create the delegate that forwards to the implementation
        var forwardDelegate = DelegateFactory.CreateReturnsDelegate(impl, method, paramTypes);

        // Set up the mock with callback and forwarding
        var setup = mock.Setup(expression);

        // Create a combined Returns delegate that:
        // 1. Calls the implementation
        // 2. Invokes the callback with params and/or result
        // 3. Returns the result
        var funcType = Expression.GetFuncType(paramTypes.Concat([typeof(TResult)]).ToArray());
        var paramExprs = paramTypes.Select((t, i) => Expression.Parameter(t, $"p{i + 1}")).ToArray();

        // Call the forward delegate to get the result
        // ReSharper disable once CoVariantArrayConversion
        var forwardCallExpr = Expression.Invoke(Expression.Constant(forwardDelegate), paramExprs);
        var resultVarExpr = Expression.Variable(typeof(TResult), "result");
        var assignResultExpr = Expression.Assign(resultVarExpr, forwardCallExpr);

        // Invoke the user callback
        Expression callbackInvokeExpr;
        var callbackParams = callback.Method.GetParameters();
        if (callbackParams.Length == 0)
        {
            // Parameterless callback
            callbackInvokeExpr = Expression.Invoke(Expression.Constant(callback));
        }
        else if (callbackParams.Length == paramTypes.Length)
        {
            // Callback with parameters only
            // ReSharper disable once CoVariantArrayConversion
            callbackInvokeExpr = Expression.Invoke(Expression.Constant(callback), paramExprs);
        }
        else if (callbackParams.Length == paramTypes.Length + 1)
        {
            // Callback with parameters and result
            var methodArgsPlusResultParamExprs = paramExprs.Concat([resultVarExpr]).ToArray();
            // ReSharper disable once CoVariantArrayConversion
            callbackInvokeExpr = Expression.Invoke(Expression.Constant(callback), methodArgsPlusResultParamExprs);
        }
        else
        {
            throw new ArgumentException(
                $"Callback must have 0, {paramTypes.Length}, or {paramTypes.Length + 1} parameters.",
                nameof(callback));
        }

        // Combine: assign result, invoke callback, return result
        var blockExpr =
            Expression.Block(
                [resultVarExpr],
                assignResultExpr,
                callbackInvokeExpr,
                resultVarExpr);

        var lambdaExpr = Expression.Lambda(funcType, blockExpr, paramExprs);
        var finalReturnsDelegate = lambdaExpr.Compile();

        // Find and invoke the Returns method with the forwarding delegate
        var returnsMethod = setup
            .GetType()
            .GetMethods()
            .FirstOrDefault(m =>
            {
                if (m.Name != nameof(ISetup<object, object>.Returns))
                {
                    return false;
                }

                var mParams = m.GetParameters();
                return mParams.Length == 1 && mParams[0].ParameterType.IsAssignableTo(typeof(Delegate));
            });

        returnsMethod?.Invoke(setup, [finalReturnsDelegate]);
    }
}