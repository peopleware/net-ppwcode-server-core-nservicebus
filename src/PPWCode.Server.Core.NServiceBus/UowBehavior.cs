using System;
using System.Data;
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
                ISession session = Kernel.Resolve<ISession>();
                try
                {
                    Contract.Assert(session.IsOpen);
                    ITransaction transaction = session.BeginTransaction(IsolationLevel.Unspecified);
                    try
                    {
                        MessageContextAccessor.MessageContext = new RequestMessageContext(context.MessageHeaders);
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
                        if (transaction.IsActive)
                        {
                            Logger.Error($"Rolling back transaction for MessageId: {context.MessageId}.");
                            await transaction.RollbackAsync().ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    Kernel.ReleaseComponent(session);
                }
            }
        }
    }
}
