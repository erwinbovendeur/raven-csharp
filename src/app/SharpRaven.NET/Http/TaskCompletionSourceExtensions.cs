namespace Mannex.Threading.Tasks
{
    #region Imports

    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="TaskCompletionSource{TResult}"/>.
    /// </summary>

    static partial class TaskCompletionSourceExtensions
    {
        /// <summary>
        /// Attempts to conclude <see cref="TaskCompletionSource{TResult}"/>
        /// as being canceled, faulted or having completed successfully
        /// based on the corresponding status of the given 
        /// <see cref="Task{T}"/>.
        /// </summary>

        public static bool TryConcludeFrom<T>(this TaskCompletionSource<T> source, Task<T> task)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (task == null) throw new ArgumentNullException("task");

            if (task.IsCanceled)
            {
                source.TrySetCanceled();
            }
            else if (task.IsFaulted)
            {
                var aggregate = task.Exception;
                Debug.Assert(aggregate != null);
                source.TrySetException(aggregate.InnerExceptions);
            }
            else if (TaskStatus.RanToCompletion == task.Status)
            {
                source.TrySetResult(task.Result);
            }
            else
            {
                return false;
            }
            return true;
        }
    }
}