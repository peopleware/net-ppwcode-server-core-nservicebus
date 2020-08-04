using JetBrains.Annotations;

namespace PPWCode.Server.Core.NServiceBus
{
    public interface IMessageContextAccessor
    {
        [CanBeNull]
        RequestMessageContext MessageContext { get; set; }
    }
}
