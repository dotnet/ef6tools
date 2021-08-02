// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Interception
{
    using System.ComponentModel;
    using System.Data.Common;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// This is the default log formatter used when some <see cref="Action{String}" /> is set onto the <see cref="Database.Log" />
    /// property. A different formatter can be used by creating a class that inherits from this class and overrides
    /// some or all methods to change behavior.
    /// </summary>
    /// <remarks>
    /// To set the new formatter create a code-based configuration for EF using <see cref="DbConfiguration" /> and then
    /// set the formatter class to use with <see cref="DbConfiguration.SetDatabaseLogFormatter" />.
    /// Note that setting the type of formatter to use with this method does change the way command are
    /// logged when <see cref="Database.Log" /> is used. It is still necessary to set a <see cref="Action{String}" />
    /// onto <see cref="Database.Log" /> before any commands will be logged.
    /// For more low-level control over logging/interception see <see cref="IDbCommandInterceptor" /> and
    /// <see cref="DbInterception" />.
    /// Interceptors can also be registered in the config file of the application.
    /// See http://go.microsoft.com/fwlink/?LinkId=260883 for more information about Entity Framework configuration.
    /// </remarks>
    public class DatabaseLogFormatter : IDbCommandInterceptor, IDbConnectionInterceptor, IDbTransactionInterceptor
    {
        private const string StopwatchStateKey = "__LoggingStopwatch__";
        private readonly WeakReference _context;
        private readonly Action<string> _writeAction;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// Creates a formatter that will not filter by any <see cref="DbContext" /> and will instead log every command
        /// from any context and also commands that do not originate from a context.
        /// </summary>
        /// <remarks>
        /// This constructor is not used when a delegate is set on <see cref="Database.Log" />. Instead it can be
        /// used by setting the formatter directly using <see cref="DbInterception.Add" />.
        /// </remarks>
        /// <param name="writeAction">The delegate to which output will be sent.</param>
        public DatabaseLogFormatter(Action<string> writeAction)
        {
            Check.NotNull(writeAction, "writeAction");

            _writeAction = writeAction;
        }

        /// <summary>
        /// Creates a formatter that will only log commands the come from the given <see cref="DbContext" /> instance.
        /// </summary>
        /// <remarks>
        /// This constructor must be called by a class that inherits from this class to override the behavior
        /// of <see cref="Database.Log" />.
        /// </remarks>
        /// <param name="context">
        /// The context for which commands should be logged. Pass null to log every command
        /// from any context and also commands that do not originate from a context.
        /// </param>
        /// <param name="writeAction">The delegate to which output will be sent.</param>
        public DatabaseLogFormatter(DbContext context, Action<string> writeAction)
        {
            Check.NotNull(writeAction, "writeAction");

            _context = new WeakReference(context);
            _writeAction = writeAction;
        }

        /// <summary>
        /// The context for which commands are being logged, or null if commands from all contexts are
        /// being logged.
        /// </summary>
        protected internal DbContext Context
        {
            get
            {
                return _context != null && _context.IsAlive
                    ? (DbContext)_context.Target
                    : null;
            }
        }

        internal Action<string> WriteAction
        {
            get { return _writeAction; }
        }

        /// <summary>
        /// Writes the given string to the underlying write delegate.
        /// </summary>
        /// <param name="output">The string to write.</param>
        protected virtual void Write(string output)
        {
            _writeAction(output);
        }

        /// <summary>
        /// This property is obsolete. Using it can result in logging incorrect execution times. Call
        /// <see cref="GetStopwatch"/> instead.
        /// </summary>
        [Obsolete("This stopwatch can give incorrect times. Use 'GetStopwatch' instead.")]
        protected internal Stopwatch Stopwatch
        {
            get { return _stopwatch; }
        }

        /// <summary>
        /// The stopwatch used to time executions. This stopwatch is started at the end of
        /// <see cref="NonQueryExecuting" />, <see cref="ScalarExecuting" />, and <see cref="ReaderExecuting" />
        /// methods and is stopped at the beginning of the <see cref="NonQueryExecuted" />, <see cref="ScalarExecuted" />,
        /// and <see cref="ReaderExecuted" /> methods. If these methods are overridden and the stopwatch is being used
        /// then the overrides should either call the base method or start/stop the stopwatch themselves.
        /// </summary>
        /// <param name="interceptionContext">The interception context for which the stopwatch will be obtained.</param>
        /// <returns>The stopwatch.</returns>
        protected internal Stopwatch GetStopwatch(DbCommandInterceptionContext interceptionContext)
        {
            if (_context != null)
            {
                return _stopwatch;
            }

            var mutableContext = (IDbMutableInterceptionContext)interceptionContext;
            var stopwatch = (Stopwatch)mutableContext.MutableData.FindUserState(StopwatchStateKey);

            if (stopwatch == null)
            {
                stopwatch = new Stopwatch();
                mutableContext.MutableData.SetUserState(StopwatchStateKey, stopwatch);
            }

            return stopwatch;
        }

        private void RestartStopwatch(DbCommandInterceptionContext interceptionContext)
        {
            var stopwatch = GetStopwatch(interceptionContext);
            stopwatch.Restart();

            // Preseve behavior for any code still using the obsolete Stopwatch property in method overrides.
            if (!ReferenceEquals(stopwatch, _stopwatch))
            {
                _stopwatch.Restart();
            }
        }

        private void StopStopwatch(DbCommandInterceptionContext interceptionContext)
        {
            var stopwatch = GetStopwatch(interceptionContext);
            stopwatch.Stop();

            // Preseve behavior for any code still using the obsolete Stopwatch property in method overrides.
            if (!ReferenceEquals(stopwatch, _stopwatch))
            {
                _stopwatch.Stop();
            }
        }

        /// <summary>
        /// This method is called before a call to <see cref="DbCommand.ExecuteNonQuery" /> or
        /// one of its async counterparts is made.
        /// The default implementation calls <see cref="Executing" /> and starts the stopwatch returned from
        /// <see cref="GetStopwatch"/>.
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            Executing(command, interceptionContext);
            RestartStopwatch(interceptionContext);
        }

        /// <summary>
        /// This method is called after a call to <see cref="DbCommand.ExecuteNonQuery" /> or
        /// one of its async counterparts is made.
        /// The default implementation stopsthe stopwatch returned from <see cref="GetStopwatch"/> and calls
        /// <see cref="Executed" />.
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            StopStopwatch(interceptionContext);
            Executed(command, interceptionContext);
        }

        /// <summary>
        /// This method is called before a call to <see cref="DbCommand.ExecuteReader(CommandBehavior)" /> or
        /// one of its async counterparts is made.
        /// The default implementation calls <see cref="Executing" /> and starts the stopwatch returned from
        /// <see cref="GetStopwatch"/>.
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            Executing(command, interceptionContext);
            RestartStopwatch(interceptionContext);
        }

        /// <summary>
        /// This method is called after a call to <see cref="DbCommand.ExecuteReader(CommandBehavior)" /> or
        /// one of its async counterparts is made.
        /// The default implementation stopsthe stopwatch returned from <see cref="GetStopwatch"/> and calls
        /// <see cref="Executed" />.
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            StopStopwatch(interceptionContext);
            Executed(command, interceptionContext);
        }

        /// <summary>
        /// This method is called before a call to <see cref="DbCommand.ExecuteScalar" />  or
        /// one of its async counterparts is made.
        /// The default implementation calls <see cref="Executing" /> and starts the stopwatch returned from
        /// <see cref="GetStopwatch"/>.
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            Executing(command, interceptionContext);
            RestartStopwatch(interceptionContext);
        }

        /// <summary>
        /// This method is called after a call to <see cref="DbCommand.ExecuteScalar" />  or
        /// one of its async counterparts is made.
        /// The default implementation stopsthe stopwatch returned from <see cref="GetStopwatch"/> and calls
        /// <see cref="Executed" />.
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            StopStopwatch(interceptionContext);
            Executed(command, interceptionContext);
        }

        /// <summary>
        /// Called whenever a command is about to be executed. The default implementation of this method
        /// filters by <see cref="DbContext" /> set into <see cref="Context" />, if any, and then calls
        /// <see cref="LogCommand" />. This method would typically only be overridden to change the
        /// context filtering behavior.
        /// </summary>
        /// <typeparam name="TResult">The type of the operation's results.</typeparam>
        /// <param name="command">The command that will be executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        public virtual void Executing<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (Context == null
                || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
            {
                LogCommand(command, interceptionContext);
            }
        }

        /// <summary>
        /// Called whenever a command has completed executing. The default implementation of this method
        /// filters by <see cref="DbContext" /> set into <see cref="Context" />, if any, and then calls
        /// <see cref="LogResult" />.  This method would typically only be overridden to change the context
        /// filtering behavior.
        /// </summary>
        /// <typeparam name="TResult">The type of the operation's results.</typeparam>
        /// <param name="command">The command that was executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        public virtual void Executed<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (Context == null
                || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
            {
                LogResult(command, interceptionContext);
            }
        }

        /// <summary>
        /// Called to log a command that is about to be executed. Override this method to change how the
        /// command is logged to <see cref="WriteAction" />.
        /// </summary>
        /// <typeparam name="TResult">The type of the operation's results.</typeparam>
        /// <param name="command">The command to be logged.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        public virtual void LogCommand<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            var commandText = command.CommandText ?? "<null>";
            if (commandText.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                Write(commandText);
            }
            else
            {
                Write(commandText);
                Write(Environment.NewLine);
            }

            if (command.Parameters != null)
            {
                foreach (var parameter in command.Parameters.OfType<DbParameter>())
                {
                    LogParameter(command, interceptionContext, parameter);
                }
            }

            Write(
                interceptionContext.IsAsync
                    ? Strings.CommandLogAsync(DateTimeOffset.Now, Environment.NewLine)
                    : Strings.CommandLogNonAsync(DateTimeOffset.Now, Environment.NewLine));
        }

        /// <summary>
        /// Called by <see cref="LogCommand" /> to log each parameter. This method can be called from an overridden
        /// implementation of <see cref="LogCommand" /> to log parameters, and/or can be overridden to
        /// change the way that parameters are logged to <see cref="WriteAction" />.
        /// </summary>
        /// <typeparam name="TResult">The type of the operation's results.</typeparam>
        /// <param name="command">The command being logged.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        /// <param name="parameter">The parameter to log.</param>
        public virtual void LogParameter<TResult>(
            DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext, DbParameter parameter)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");
            Check.NotNull(parameter, "parameter");

            // -- Name: [Value] (Type = {}, Direction = {}, IsNullable = {}, Size = {}, Precision = {} Scale = {})
            var builder = new StringBuilder();
            builder.Append("-- ")
                .Append(parameter.ParameterName)
                .Append(": '")
                .Append((parameter.Value == null || parameter.Value == DBNull.Value) ? "null" : parameter.Value)
                .Append("' (Type = ")
                .Append(parameter.DbType);

            if (parameter.Direction != ParameterDirection.Input)
            {
                builder.Append(", Direction = ").Append(parameter.Direction);
            }

            if (!parameter.IsNullable)
            {
                builder.Append(", IsNullable = false");
            }

            if (parameter.Size != 0)
            {
                builder.Append(", Size = ").Append(parameter.Size);
            }

            if (((IDbDataParameter)parameter).Precision != 0)
            {
                builder.Append(", Precision = ").Append(((IDbDataParameter)parameter).Precision);
            }

            if (((IDbDataParameter)parameter).Scale != 0)
            {
                builder.Append(", Scale = ").Append(((IDbDataParameter)parameter).Scale);
            }

            builder.Append(")").Append(Environment.NewLine);

            Write(builder.ToString());
        }

        /// <summary>
        /// Called to log the result of executing a command. Override this method to change how results are
        /// logged to <see cref="WriteAction" />.
        /// </summary>
        /// <typeparam name="TResult">The type of the operation's results.</typeparam>
        /// <param name="command">The command being logged.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        public virtual void LogResult<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            var stopwatch = _stopwatch;
            if (_context == null)
            {
                var safeStopwatch = (Stopwatch)((IDbMutableInterceptionContext)interceptionContext).MutableData
                     .FindUserState(StopwatchStateKey);

                // If overriding methods still use obsolete Stopwatch, then preserve this behavior to avoid
                // breaking change.
                if (safeStopwatch != null)
                {
                    stopwatch = safeStopwatch;
                }
            }

            if (interceptionContext.Exception != null)
            {
                Write(
                    Strings.CommandLogFailed(
                        stopwatch.ElapsedMilliseconds, interceptionContext.Exception.Message, Environment.NewLine));
            }
            else if (interceptionContext.TaskStatus.HasFlag(TaskStatus.Canceled))
            {
                Write(Strings.CommandLogCanceled(stopwatch.ElapsedMilliseconds, Environment.NewLine));
            }
            else
            {
                var result = interceptionContext.Result;
                var resultString = (object)result == null
                    ? "null"
                    : (result is DbDataReader)
                        ? result.GetType().Name
                        : result.ToString();
                Write(Strings.CommandLogComplete(stopwatch.ElapsedMilliseconds, resultString, Environment.NewLine));
            }

            Write(Environment.NewLine);
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection beginning the transaction.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void BeginningTransaction(DbConnection connection, BeginTransactionInterceptionContext interceptionContext)
        {
        }

        /// <summary>
        /// Called after <see cref="DbConnection.BeginTransaction(Data.IsolationLevel)" /> is invoked.
        /// The default implementation of this method filters by <see cref="DbContext" /> set into
        /// <see cref="Context" />, if any, and then logs the event.
        /// </summary>
        /// <param name="connection">The connection that began the transaction.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void BeganTransaction(DbConnection connection, BeginTransactionInterceptionContext interceptionContext)
        {
            Check.NotNull(connection, "connection");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (Context == null
                || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
            {
                if (interceptionContext.Exception != null)
                {
                    Write(Strings.TransactionStartErrorLog(DateTimeOffset.Now, interceptionContext.Exception.Message, Environment.NewLine));
                }
                else
                {
                    Write(Strings.TransactionStartedLog(DateTimeOffset.Now, Environment.NewLine));
                }
            }
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void EnlistingTransaction(DbConnection connection, EnlistTransactionInterceptionContext interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void EnlistedTransaction(DbConnection connection, EnlistTransactionInterceptionContext interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection being opened.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Opening(DbConnection connection, DbConnectionInterceptionContext interceptionContext)
        {
        }

        /// <summary>
        /// Called after <see cref="DbConnection.Open" /> or its async counterpart is invoked.
        /// The default implementation of this method filters by <see cref="DbContext" /> set into
        /// <see cref="Context" />, if any, and then logs the event.
        /// </summary>
        /// <param name="connection">The connection that was opened.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Opened(DbConnection connection, DbConnectionInterceptionContext interceptionContext)
        {
            Check.NotNull(connection, "connection");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (Context == null
                || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
            {
                if (interceptionContext.Exception != null)
                {
                    Write(
                        interceptionContext.IsAsync
                            ? Strings.ConnectionOpenErrorLogAsync(
                                DateTimeOffset.Now, interceptionContext.Exception.Message, Environment.NewLine)
                            : Strings.ConnectionOpenErrorLog(DateTimeOffset.Now, interceptionContext.Exception.Message, Environment.NewLine));
                }
                else if (interceptionContext.TaskStatus.HasFlag(TaskStatus.Canceled))
                {
                    Write(Strings.ConnectionOpenCanceledLog(DateTimeOffset.Now, Environment.NewLine));
                }
                else
                {
                    Write(
                        interceptionContext.IsAsync
                            ? Strings.ConnectionOpenedLogAsync(DateTimeOffset.Now, Environment.NewLine)
                            : Strings.ConnectionOpenedLog(DateTimeOffset.Now, Environment.NewLine));
                }
            }
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection being closed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Closing(DbConnection connection, DbConnectionInterceptionContext interceptionContext)
        {
        }

        /// <summary>
        /// Called after <see cref="DbConnection.Close" /> is invoked.
        /// The default implementation of this method filters by <see cref="DbContext" /> set into
        /// <see cref="Context" />, if any, and then logs the event.
        /// </summary>
        /// <param name="connection">The connection that was closed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Closed(DbConnection connection, DbConnectionInterceptionContext interceptionContext)
        {
            Check.NotNull(connection, "connection");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (Context == null
                || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
            {
                if (interceptionContext.Exception != null)
                {
                    Write(Strings.ConnectionCloseErrorLog(DateTimeOffset.Now, interceptionContext.Exception.Message, Environment.NewLine));
                }
                else
                {
                    Write(Strings.ConnectionClosedLog(DateTimeOffset.Now, Environment.NewLine));
                }
            }
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ConnectionStringGetting(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ConnectionStringGot(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ConnectionStringSetting(
            DbConnection connection, DbConnectionPropertyInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ConnectionStringSet(
            DbConnection connection, DbConnectionPropertyInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ConnectionTimeoutGetting(DbConnection connection, DbConnectionInterceptionContext<int> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ConnectionTimeoutGot(DbConnection connection, DbConnectionInterceptionContext<int> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void DatabaseGetting(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void DatabaseGot(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void DataSourceGetting(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void DataSourceGot(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Called before <see cref="Component.Dispose()" /> is invoked.
        /// The default implementation of this method filters by <see cref="DbContext" /> set into
        /// <see cref="Context" />, if any, and then logs the event.
        /// </summary>
        /// <param name="connection">The connection being disposed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Disposing(DbConnection connection, DbConnectionInterceptionContext interceptionContext)
        {
            Check.NotNull(connection, "connection");
            Check.NotNull(interceptionContext, "interceptionContext");

            if ((Context == null
                 || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
                && connection.State == ConnectionState.Open)
            {
                Write(Strings.ConnectionDisposedLog(DateTimeOffset.Now, Environment.NewLine));
            }
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection that was disposed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Disposed(DbConnection connection, DbConnectionInterceptionContext interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ServerVersionGetting(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ServerVersionGot(DbConnection connection, DbConnectionInterceptionContext<string> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void StateGetting(DbConnection connection, DbConnectionInterceptionContext<ConnectionState> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void StateGot(DbConnection connection, DbConnectionInterceptionContext<ConnectionState> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ConnectionGetting(DbTransaction transaction, DbTransactionInterceptionContext<DbConnection> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ConnectionGot(DbTransaction transaction, DbTransactionInterceptionContext<DbConnection> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden. </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void IsolationLevelGetting(
            DbTransaction transaction, DbTransactionInterceptionContext<IsolationLevel> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void IsolationLevelGot(
            DbTransaction transaction, DbTransactionInterceptionContext<IsolationLevel> interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="transaction">The transaction being commited.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Committing(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        /// <summary>
        /// This method is called after <see cref="DbTransaction.Commit" /> is invoked.
        /// The default implementation of this method filters by <see cref="DbContext" /> set into
        /// <see cref="Context" />, if any, and then logs the event.
        /// </summary>
        /// <param name="transaction">The transaction that was commited.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Committed(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
            Check.NotNull(transaction, "transaction");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (Context == null
                || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
            {
                if (interceptionContext.Exception != null)
                {
                    Write(Strings.TransactionCommitErrorLog(DateTimeOffset.Now, interceptionContext.Exception.Message, Environment.NewLine));
                }
                else
                {
                    Write(Strings.TransactionCommittedLog(DateTimeOffset.Now, Environment.NewLine));
                }
            }
        }

        /// <summary>
        /// This method is called before <see cref="DbTransaction.Dispose()" /> is invoked.
        /// The default implementation of this method filters by <see cref="DbContext" /> set into
        /// <see cref="Context" />, if any, and then logs the event.
        /// </summary>
        /// <param name="transaction">The transaction being disposed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Disposing(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
            Check.NotNull(transaction, "transaction");
            Check.NotNull(interceptionContext, "interceptionContext");

            if ((Context == null
                 || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
                && transaction.Connection != null)
            {
                Write(Strings.TransactionDisposedLog(DateTimeOffset.Now, Environment.NewLine));
            }
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="transaction">The transaction that was disposed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void Disposed(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        /// <summary>
        /// Does not write to log unless overridden.
        /// </summary>
        /// <param name="transaction">The transaction being rolled back.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void RollingBack(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        /// <summary>
        /// This method is called after <see cref="DbTransaction.Rollback" /> is invoked.
        /// The default implementation of this method filters by <see cref="DbContext" /> set into
        /// <see cref="Context" />, if any, and then logs the event.
        /// </summary>
        /// <param name="transaction">The transaction that was rolled back.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void RolledBack(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
            Check.NotNull(transaction, "transaction");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (Context == null
                || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
            {
                if (interceptionContext.Exception != null)
                {
                    Write(
                        Strings.TransactionRollbackErrorLog(DateTimeOffset.Now, interceptionContext.Exception.Message, Environment.NewLine));
                }
                else
                {
                    Write(Strings.TransactionRolledBackLog(DateTimeOffset.Now, Environment.NewLine));
                }
            }
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}
