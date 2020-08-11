using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NHibernate;
using NServiceBus;
using PPWCode.API.Core.Contracts;

namespace PPWCode.Server.Core.NServiceBus
{
    public class RequestMessageContext
    {
        public RequestMessageContext(
            [NotNull] IReadOnlyDictionary<string, string> messageHeaders,
            [NotNull] ISession session,
            [NotNull] ITransaction transaction)
        {
            Contract.Assert(session.IsOpen);
            Contract.Assert(transaction.IsActive);

            MessageHeaders = messageHeaders;
            Session = session;
            Transaction = transaction;
        }

        [NotNull]
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }

        [NotNull] public ISession Session { get; }

        [NotNull] public ITransaction Transaction { get; }

        [CanBeNull]
        public string MessageId
            => MessageHeaders.ContainsKey(Headers.MessageId)
                   ? MessageHeaders[Headers.MessageId]
                   : null;

        [CanBeNull]
        public string CorrelationId
            => MessageHeaders.ContainsKey(Headers.CorrelationId)
                   ? MessageHeaders[Headers.CorrelationId]
                   : null;

        [CanBeNull]
        public DateTime? TimeSent
            => MessageHeaders.ContainsKey(Headers.TimeSent)
                   ? DateTimeExtensions.ToUtcDateTime(MessageHeaders[Headers.TimeSent])
                   : (DateTime?)null;
    }
}
