// Copyright 2024 by PeopleWare n.v..
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

        [NotNull]
        public ISession Session { get; }

        [NotNull]
        public ITransaction Transaction { get; }

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
                   : null;
    }
}
