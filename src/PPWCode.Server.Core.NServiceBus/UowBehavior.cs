using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Castle.MicroKernel;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Lifestyle.Scoped;
using JetBrains.Annotations;
using NHibernate;
using NServiceBus;
using NServiceBus.Pipeline;
using PPWCode.API.Core.Contracts;
using PPWCode.API.Core.Exceptions;

namespace PPWCode.Server.Core.NServiceBus
{
    /// <inheritdoc />
    [UsedImplicitly]
    public class UowBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public UowBehavior(
            [NotNull] IKernel kernel,
            [NotNull] IMessageContextAccessor messageContextAccessor,
            [NotNull] ILogger logger)
        {
            Kernel = kernel;
            MessageContextAccessor = messageContextAccessor;
            Logger = logger;
        }

        [NotNull]
        public IKernel Kernel { get; }

        [NotNull]
        public IMessageContextAccessor MessageContextAccessor { get; }

        [NotNull]
        public ILogger Logger { get; }

        /// <inheritdoc />
        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            CallContextLifetimeScope currentScope = CallContextLifetimeScope.ObtainCurrentScope();
            if (currentScope != null)
            {
                throw new InternalProgrammingError($"Didn't expect a Castle-Windsor scope yet, for MessageId: {context.MessageId}");
            }

            using (Kernel.BeginScope())
            {
                bool measure = Logger.IsInfoEnabled;
                Stopwatch sw = null;
                string traceIdentifier = null;
                if (measure)
                {
                    sw = Stopwatch.StartNew();
                }

                ISession session = Kernel.Resolve<ISession>();
                try
                {
                    Contract.Assert(session.IsOpen);
                    ITransaction transaction = session.BeginTransaction(IsolationLevel.Unspecified);
                    try
                    {
                        MessageContextAccessor.MessageContext =
                            new RequestMessageContext(
                                context.MessageHeaders,
                                session,
                                transaction);
                        if (measure)
                        {
                            traceIdentifier = MessageContextAccessor.MessageContext?.CorrelationId;
                        }

                        try
                        {
                            await next().ConfigureAwait(false);
                        }
                        finally
                        {
                            MessageContextAccessor.MessageContext = null;
                        }

                        Logger.Info(() => $"Flush and commit our request transaction, for MessageId: {context.MessageId}.");
                        await session.FlushAsync().ConfigureAwait(false);
                        await transaction.CommitAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Info($"Operation cancelled, for MessageId: {context.MessageId}.");
                        throw;
                    }
                    catch (MessageDeserializationException)
                    {
                        Logger.Info($"Message deserialization exception for MessageId: {context.MessageId}.");
                        throw;
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"While flush and committing for MessageId: {context.MessageId}.", e);
                        throw;
                    }
                    finally
                    {
                        try
                        {
                            if (transaction.IsActive)
                            {
                                Logger.Error($"Rolling back transaction for MessageId: {context.MessageId}.");
                                await transaction.RollbackAsync().ConfigureAwait(false);
                            }
                        }
                        catch (Exception e2)
                        {
                            Logger.Error($"Rollback of the transaction failed for MessageId: {context.MessageId}.", e2);
                        }
                    }
                }
                finally
                {
                    Kernel.ReleaseComponent(session);

                    if (measure)
                    {
                        sw.Stop();
                        Logger.Info(() => $"Message [MessageId: {context.MessageId}, TraceIdentifier: {traceIdentifier}] was processed in {sw.ElapsedMilliseconds} ms.");
                    }
                }
            }
        }
    }
}
