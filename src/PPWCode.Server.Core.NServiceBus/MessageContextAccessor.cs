using System.Threading;
using JetBrains.Annotations;

namespace PPWCode.Server.Core.NServiceBus
{
    /// <inheritdoc cref="IMessageContextAccessor" />
    [UsedImplicitly]
    public class MessageContextAccessor : IMessageContextAccessor
    {
        private static readonly AsyncLocal<MessageContextHolder> RequestMessageContextCurrent =
            new AsyncLocal<MessageContextHolder>();

        public RequestMessageContext MessageContext
        {
            get => RequestMessageContextCurrent.Value?.Context;
            set
            {
                MessageContextHolder holder = RequestMessageContextCurrent.Value;
                if (holder != null)
                {
                    // Clear current HttpContext trapped in the AsyncLocals, as its done.
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the HttpContext in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    RequestMessageContextCurrent.Value = new MessageContextHolder { Context = value };
                }
            }
        }

        private class MessageContextHolder
        {
            public RequestMessageContext Context { get; set; }
        }
    }
}
