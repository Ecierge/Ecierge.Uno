namespace Ecierge.Uno.Navigation;

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.UI.Dispatching;

public static class DispatcherQueueExtensions
{
    private static readonly bool IsHasThreadAccessPropertyAvailable = true;

    public static ValueTask EnqueueAsync(this DispatcherQueue dispatcher, Action function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (IsHasThreadAccessPropertyAvailable && dispatcher.HasThreadAccess)
        {
            function();
            return ValueTask.CompletedTask;
        }

        return TryEnqueueAsync(dispatcher, function, priority);
        static ValueTask TryEnqueueAsync(DispatcherQueue dispatcher, Action function, DispatcherQueuePriority priority)
        {
            var taskCompletionSource = PoolingAsyncValueTaskMethodBuilder.Create();
            if (!dispatcher.TryEnqueue(priority, delegate
            {
                try
                {
                    function();
                    taskCompletionSource.SetResult();
                }
                catch (Exception exception)
                {
                    taskCompletionSource.SetException(exception);
                }
            }))
            {
                taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
            }

            return taskCompletionSource.Task;
        }
    }

    public static ValueTask<T> EnqueueAsync<T>(this DispatcherQueue dispatcher, Func<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (IsHasThreadAccessPropertyAvailable && dispatcher.HasThreadAccess)
        {
            return ValueTask.FromResult(function());
        }

        return TryEnqueueAsync(dispatcher, function, priority);
        static ValueTask<T> TryEnqueueAsync(DispatcherQueue dispatcher, Func<T> function, DispatcherQueuePriority priority)
        {
            var taskCompletionSource = PoolingAsyncValueTaskMethodBuilder<T>.Create();
            if (!dispatcher.TryEnqueue(priority, delegate
            {
                try
                {
                    taskCompletionSource.SetResult(function());
                }
                catch (Exception exception)
                {
                    taskCompletionSource.SetException(exception);
                }
            }))
            {
                taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
            }

            return taskCompletionSource.Task;
        }
    }

    public static ValueTask EnqueueAsync(this DispatcherQueue dispatcher, Func<ValueTask> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (IsHasThreadAccessPropertyAvailable && dispatcher.HasThreadAccess)
        {
            return function();
        }

        return TryEnqueueAsync(dispatcher, function, priority);
        static ValueTask TryEnqueueAsync(DispatcherQueue dispatcher, Func<ValueTask> function, DispatcherQueuePriority priority)
        {
            var taskCompletionSource = PoolingAsyncValueTaskMethodBuilder.Create();
            if (!dispatcher.TryEnqueue(priority, async delegate
            {
                try
                {
                    await function().ConfigureAwait(false);
                    taskCompletionSource.SetResult();
                }
                catch (Exception exception)
                {
                    taskCompletionSource.SetException(exception);
                }
            }))
            {
                taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
            }

            return taskCompletionSource.Task;
        }
    }

    public static ValueTask<T> EnqueueAsync<T>(this DispatcherQueue dispatcher, Func<ValueTask<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (IsHasThreadAccessPropertyAvailable && dispatcher.HasThreadAccess)
        {
            return function();
        }

        return TryEnqueueAsync(dispatcher, function, priority);
        static ValueTask<T> TryEnqueueAsync(DispatcherQueue dispatcher, Func<ValueTask<T>> function, DispatcherQueuePriority priority)
        {
            Func<ValueTask<T>> function2 = function;
            PoolingAsyncValueTaskMethodBuilder<T> taskCompletionSource = PoolingAsyncValueTaskMethodBuilder<T>.Create();
            if (!dispatcher.TryEnqueue(priority, async delegate
            {
                try
                {
                    ValueTask<T> task2 = function2();
                    T result = await task2.ConfigureAwait(continueOnCapturedContext: false);
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception exception2)
                {
                    taskCompletionSource.SetException(exception2);
                }
            }))
            {
                taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
            }

            return taskCompletionSource.Task;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InvalidOperationException GetEnqueueException(string message)
    {
        return new InvalidOperationException(message);
    }
}
