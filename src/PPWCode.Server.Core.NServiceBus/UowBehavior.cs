// Copyright 2026 by PeopleWare n.v..
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using NHibernate;

using NServiceBus;
using NServiceBus.Pipeline;

using PPWCode.Vernacular.Contracts.I;
using PPWCode.Vernacular.NHibernate.III.Async.Interfaces.Providers;

namespace PPWCode.Server.Core.NServiceBus
{
    /// <inheritdoc />
    [UsedImplicitly]
    public class UowBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public UowBehavior(
            [NotNull] ILogger<UowBehavior> logger,
            [NotNull] IMessageContextAccessor messageContextAccessor)
        {
            Logger = logger;
            MessageContextAccessor = messageContextAccessor;
        }

        [NotNull]
        public IMessageContextAccessor MessageContextAccessor { get; }

        [NotNull]
        public ILogger Logger { get; }

        /// <inheritdoc />
        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            bool measure = Logger.IsEnabled(LogLevel.Information);
            Stopwatch sw = null;
            string traceIdentifier = null;
            if (measure)
            {
                sw = Stopwatch.StartNew();
            }

            ISessionProviderAsync sessionProvider = context.Builder.Build<ISessionProviderAsync>();
            ISession session = sessionProvider.Session;
            try
            {
                Contract.Assert(session.IsOpen);
                if (Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation("Start transaction for message {MessageId}", context.MessageId);
                }

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
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation("Flush and commit our request transaction, for MessageId {MessageId}", context.MessageId);
                        }

                        await session.FlushAsync().ConfigureAwait(false);
                        await transaction.CommitAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        MessageContextAccessor.MessageContext = null;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation("Operation cancelled, for MessageId {MessageId}", context.MessageId);
                    }

                    throw;
                }
                catch (MessageDeserializationException)
                {
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation("Message deserialization exception for MessageId {MessageId}", context.MessageId);
                    }

                    throw;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "While flush and committing for MessageId {MessageId}", context.MessageId);
                    throw;
                }
                finally
                {
                    try
                    {
                        if (transaction.IsActive)
                        {
                            Logger.LogError("Rolling back transaction for MessageId {MessageId}", context.MessageId);
                            await transaction.RollbackAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception e2)
                    {
                        Logger.LogError(e2, "Rollback of the transaction failed for MessageId {MessageId}", context.MessageId);
                    }
                }
            }
            finally
            {
                if (measure)
                {
                    sw.Stop();
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation("Message deserialization exception for MessageId {MessageId}", context.MessageId);
                        Logger.LogInformation(
                            "Message [MessageId: {MessageId}, TraceIdentifier: {TraceIdentifier}] was processed in {ElapsedMilliseconds} ms",
                            context.MessageId,
                            traceIdentifier,
                            sw.ElapsedMilliseconds);
                    }
                }
            }
        }
    }
}
