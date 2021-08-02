﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if !NET40
#if SQLSERVER
namespace System.Data.Entity.SqlServer.Utilities
#else
namespace System.Data.Entity.Utilities
#endif
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Contains extension methods for the <see cref="Task" /> class.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Configures an awaiter used to await this <see cref="Task{TResult}" /> to avoid
        /// marshalling the continuation
        /// back to the original context, but preserve the current culture and UI culture.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the result produced by the associated <see cref="Task{TResult}"/>.
        /// </typeparam>
        /// <param name="task">The task to be awaited on.</param>
        /// <returns>An object used to await this task.</returns>
        public static CultureAwaiter<T> WithCurrentCulture<T>(this Task<T> task)
        {
            return new CultureAwaiter<T>(task);
        }

        /// <summary>
        /// Configures an awaiter used to await this <see cref="Task" /> to avoid
        /// marshalling the continuation
        /// back to the original context, but preserve the current culture and UI culture.
        /// </summary>
        /// <param name="task">The task to be awaited on.</param>
        /// <returns>An object used to await this task.</returns>
        public static CultureAwaiter WithCurrentCulture(this Task task)
        {
            return new CultureAwaiter(task);
        }

        /// <summary>
        /// Provides an awaitable object that allows for awaits on <see cref="Task{TResult}" /> that
        /// preserve the culture.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the result produced by the associated <see cref="Task{TResult}"/>.
        /// </typeparam>
        /// <remarks>This type is intended for compiler use only.</remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Awaiter")]
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
        public struct CultureAwaiter<T> : ICriticalNotifyCompletion
        {
            private readonly Task<T> _task;

            /// <summary>
            /// Constructs a new instance of the <see cref="CultureAwaiter{T}" /> class.
            /// </summary>
            /// <param name="task">The task to be awaited on.</param>
            public CultureAwaiter(Task<T> task)
            {
                _task = task;
            }

            /// <summary>Gets an awaiter used to await this <see cref="Task{TResult}" />.</summary>
            /// <returns>An awaiter instance.</returns>
            /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
            [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Awaiter")]
            [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
            public CultureAwaiter<T> GetAwaiter()
            {
                return this;
            }

            /// <summary>
            /// Gets whether this <see cref="Task">Task</see> has completed.
            /// </summary>
            /// <remarks>
            /// <see cref="IsCompleted" /> will return true when the Task is in one of the three
            /// final states: <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>,
            /// <see cref="TaskStatus.Faulted">Faulted</see>, or
            /// <see cref="TaskStatus.Canceled">Canceled</see>.
            /// </remarks>
            public bool IsCompleted
            {
                get { return _task.IsCompleted; }
            }

            /// <summary>Ends the await on the completed <see cref="Task{TResult}" />.</summary>
            /// <returns>The result of the completed <see cref="Task{TResult}" />.</returns>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <exception cref="TaskCanceledException">The task was canceled.</exception>
            /// <exception cref="System.Exception">The task completed in a Faulted state.</exception>
            [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
            public T GetResult()
            {
                return _task.GetAwaiter().GetResult();
            }

            /// <summary>This method is not implemented and should not be called.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="Task{TResult}" /> associated with this
            /// <see cref="TaskAwaiter{TResult}" />.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="System.ArgumentNullException">
            /// The <paramref name="continuation" /> argument is null
            /// (Nothing in Visual Basic).
            /// </exception>
            /// <exception cref="System.InvalidOperationException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
            public void UnsafeOnCompleted(Action continuation)
            {
                var currentCulture = Thread.CurrentThread.CurrentCulture;
                var currentUICulture = Thread.CurrentThread.CurrentUICulture;
                _task.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().UnsafeOnCompleted(
                    () =>
                    {
                        var originalCulture = Thread.CurrentThread.CurrentCulture;
                        var originalUICulture = Thread.CurrentThread.CurrentUICulture;
                        Thread.CurrentThread.CurrentCulture = currentCulture;
                        Thread.CurrentThread.CurrentUICulture = currentUICulture;
                        try
                        {
                            continuation();
                        }
                        finally
                        {
                            Thread.CurrentThread.CurrentCulture = originalCulture;
                            Thread.CurrentThread.CurrentUICulture = originalUICulture;
                        }
                    });
            }
        }

        /// <summary>
        /// Provides an awaitable object that allows for awaits on <see cref="Task" /> that
        /// preserve the culture.
        /// </summary>
        /// <remarks>This type is intended for compiler use only.</remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Awaiter")]
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
        public struct CultureAwaiter : ICriticalNotifyCompletion
        {
            private readonly Task _task;

            /// <summary>
            /// Constructs a new instance of the <see cref="CultureAwaiter" /> class.
            /// </summary>
            /// <param name="task">The task to be awaited on.</param>
            public CultureAwaiter(Task task)
            {
                _task = task;
            }

            /// <summary>Gets an awaiter used to await this <see cref="Task" />.</summary>
            /// <returns>An awaiter instance.</returns>
            /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
            [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Awaiter")]
            [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
            public CultureAwaiter GetAwaiter()
            {
                return this;
            }

            /// <summary>
            /// Gets whether this <see cref="Task">Task</see> has completed.
            /// </summary>
            /// <remarks>
            /// <see cref="IsCompleted" /> will return true when the Task is in one of the three
            /// final states: <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>,
            /// <see cref="TaskStatus.Faulted">Faulted</see>, or
            /// <see cref="TaskStatus.Canceled">Canceled</see>.
            /// </remarks>
            public bool IsCompleted
            {
                get { return _task.IsCompleted; }
            }

            /// <summary>Ends the await on the completed <see cref="Task" />.</summary>
            /// <exception cref="System.NullReferenceException">The awaiter was not properly initialized.</exception>
            /// <exception cref="TaskCanceledException">The task was canceled.</exception>
            /// <exception cref="System.Exception">The task completed in a Faulted state.</exception>
            public void GetResult()
            {
                _task.GetAwaiter().GetResult();
            }

            /// <summary>This method is not implemented and should not be called.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Schedules the continuation onto the <see cref="Task" /> associated with this
            /// <see cref="TaskAwaiter" />.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <exception cref="System.ArgumentNullException">
            /// The <paramref name="continuation" /> argument is null
            /// (Nothing in Visual Basic).
            /// </exception>
            /// <exception cref="System.InvalidOperationException">The awaiter was not properly initialized.</exception>
            /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
            public void UnsafeOnCompleted(Action continuation)
            {
                var currentCulture = Thread.CurrentThread.CurrentCulture;
                var currentUICulture = Thread.CurrentThread.CurrentUICulture;
                _task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(
                    () =>
                    {
                        var originalCulture = Thread.CurrentThread.CurrentCulture;
                        var originalUICulture = Thread.CurrentThread.CurrentUICulture;
                        Thread.CurrentThread.CurrentCulture = currentCulture;
                        Thread.CurrentThread.CurrentUICulture = currentUICulture;
                        try
                        {
                            continuation();
                        }
                        finally
                        {
                            Thread.CurrentThread.CurrentCulture = originalCulture;
                            Thread.CurrentThread.CurrentUICulture = originalUICulture;
                        }
                    });
            }
        }
    }
}

#endif
