using System.Threading.Tasks;
using Castle.Core.Logging;
using JetBrains.Annotations;
using NServiceBus;
using PPWCode.Server.Core.RequestContext.Interfaces;

namespace PPWCode.Server.Core.NServiceBus
{
    public abstract class MessageHandler<TMessage> : IHandleMessages<TMessage>
    {
        private ILogger _logger = NullLogger.Instance;

        protected MessageHandler([NotNull] IRequestContext requestContext)
        {
            RequestContext = requestContext;
        }

        [NotNull]
        public IRequestContext RequestContext { get; }

        [NotNull]
        [UsedImplicitly]
        public ILogger Logger
        {
            get => _logger;
            set
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (value != null)
                {
                    _logger = value;
                }
            }
        }

        [CanBeNull]
        [UsedImplicitly]
        public IMessageContextAccessor MessageContextAccessor { get; set; }

        /// <inheritdoc />
        public abstract Task Handle(TMessage message, IMessageHandlerContext context);
    }
}
