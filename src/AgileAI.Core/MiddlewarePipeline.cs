using System.Collections.Generic;

namespace AgileAI.Core;

internal static class MiddlewarePipeline
{
    public static Task<TResult> ExecuteAsync<TMiddleware, TContext, TResult>(
        IEnumerable<TMiddleware> middlewares,
        TContext context,
        Func<TMiddleware, TContext, Func<Task<TResult>>, CancellationToken, Task<TResult>> invoke,
        Func<Task<TResult>> terminal,
        CancellationToken cancellationToken)
    {
        var pipeline = middlewares.Reverse()
            .Aggregate(terminal, (next, middleware) => () => invoke(middleware, context, next, cancellationToken));

        return pipeline();
    }

    public static IAsyncEnumerable<TResult> ExecuteStreaming<TMiddleware, TContext, TResult>(
        IEnumerable<TMiddleware> middlewares,
        TContext context,
        Func<TMiddleware, TContext, Func<IAsyncEnumerable<TResult>>, CancellationToken, IAsyncEnumerable<TResult>> invoke,
        Func<IAsyncEnumerable<TResult>> terminal,
        CancellationToken cancellationToken)
    {
        var pipeline = middlewares.Reverse()
            .Aggregate(terminal, (next, middleware) => () => invoke(middleware, context, next, cancellationToken));

        return pipeline();
    }
}
