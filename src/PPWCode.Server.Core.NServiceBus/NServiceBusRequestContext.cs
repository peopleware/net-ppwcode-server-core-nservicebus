using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using JetBrains.Annotations;
using PPWCode.Server.Core.RequestContext.Implementations;
using PPWCode.Vernacular.Exceptions.IV;
using PPWCode.Vernacular.Persistence.IV;

#pragma warning disable CA1065

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
