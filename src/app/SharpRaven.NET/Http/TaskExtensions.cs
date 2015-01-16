namespace Mannex.Threading.Tasks
{
    #region Imports

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="Task"/>.
    /// </summary>

    static partial class TaskExtensions
    {
        /// <summary>
        /// Returns a <see cref="Task{T}"/> that can be used as the
        /// <see cref="IAsyncResult"/> return value from the method
        /// that begin the operation of an API following the 
        /// <a href="http://msdn.microsoft.com/en-us/library/ms228963.aspx">Asynchronous Programming Model</a>.
        /// If an <see cref="AsyncCallback"/> is supplied, it is invoked
        /// when the supplied task concludes (fails, cancels or completes
        /// successfully).
        /// </summary>

        public static Task<T> Apmize<T>(this Task<T> task, AsyncCallback callback, object state)
        {
            return Apmize(task, callback, state, null);
        }

        /// <summary>
        /// Returns a <see cref="Task{T}"/> that can be used as the
        /// <see cref="IAsyncResult"/> return value from the method
        /// that begin the operation of an API following the 
        /// <a href="http://msdn.microsoft.com/en-us/library/ms228963.aspx">Asynchronous Programming Model</a>.
        /// If an <see cref="AsyncCallback"/> is supplied, it is invoked
        /// when the supplied task concludes (fails, cancels or completes
        /// successfully).
        /// </summary>

        public static Task<T> Apmize<T>(this Task<T> task, AsyncCallback callback, object state, TaskScheduler scheduler)
        {
            var result = task;

            TaskCompletionSource<T> tcs = null;
            if (task.AsyncState != state)
            {
                tcs = new TaskCompletionSource<T>(state);
                result = tcs.Task;
            }

            Task t = task;
            if (tcs != null)
            {
                t = t.ContinueWith(delegate { tcs.TryConcludeFrom(task); },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            if (callback != null)
            {
                // ReSharper disable RedundantAssignment
                t = t.ContinueWith(delegate { callback(result); }, // ReSharper restore RedundantAssignment
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    scheduler ?? TaskScheduler.Default);
            }

            return result;
        }
    }
}