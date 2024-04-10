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
using System.Security.Principal;
using System.Threading;

using JetBrains.Annotations;

using PPWCode.Server.Core.RequestContext.Implementations;
using PPWCode.Vernacular.Exceptions.IV;
using PPWCode.Vernacular.Persistence.IV;

namespace PPWCode.Server.Core.NServiceBus
{
    /// <inheritdoc />
    [UsedImplicitly]
    public class NServiceBusRequestContext : AbstractRequestContext
    {
        private IPrincipal _principal;
        private string _traceIdentifier;

        public NServiceBusRequestContext(
            [NotNull] ITimeProvider timeProvider,
            [NotNull] IMessageContextAccessor messageContextAccessor)
            : base(timeProvider)
        {
            MessageContextAccessor = messageContextAccessor;
        }

        [NotNull]
        public IMessageContextAccessor MessageContextAccessor { get; }

        /// <inheritdoc />
        public override IPrincipal User
            => (_principal ??= Thread.CurrentPrincipal)
               ?? throw new ProgrammingError("Euh, no principal found on current thread");

        /// <inheritdoc />
        public override string TraceIdentifier
            => _traceIdentifier ??= MessageContextAccessor.MessageContext?.CorrelationId
                                    ?? Guid.NewGuid().ToString("D");

        /// <inheritdoc />
        public override CancellationToken RequestAborted
            => CancellationToken.None;

        /// <inheritdoc />
        public override bool IsReadOnly
            => false;

        /// <inheritdoc />
        public override string Link(string routeName, IDictionary<string, object> values)
            => null;
    }
}
