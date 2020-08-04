using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NServiceBus;

namespace PPWCode.Server.Core.NServiceBus
{
    public class RequestMessageContext
    {
        public RequestMessageContext(
            [NotNull] IReadOnlyDictionary<string, string> messageHeaders)
        {
            MessageHeaders = messageHeaders;
        }

        [NotNull]
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }

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
